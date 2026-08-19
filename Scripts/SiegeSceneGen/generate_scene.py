# -*- coding: utf-8 -*-
"""
Gerador da cena de cerco (castelo do esboco do autor, kit Vlandia). v2, 2026-08-19.

Licoes do bisect (t1/t2):
  - terreno de cena SHIPPED e despido de dados de edicao -> o editor mostra agua.
    Solucao: MODO INJECT — o autor cria uma cena em branco no editor (terreno editavel
    de verdade) e este script injeta as entidades/paths NELA, preservando o terreno.
  - "Breaking Prefab Prevented": instancia de prefab nao-quebravel NAO pode receber
    name=/tags. Politica v2: referencias leves sem name e sem tags (exceto
    Camera_Instance, comprovada no vanilla); quem precisa de tags/overrides por
    instancia (muralhas assaltaveis, portoes) e emitido DESEMPACOTADO
    (old_prefab_name= + bloco inline copiado do arquivo de prefabs), como o vanilla faz.

Uso:
  python generate_scene.py inject <nome_da_cena>   # injeta na cena criada no editor
  python generate_scene.py 1|2|3                   # modo legado (doador; terreno nao renderiza)
"""
import io, math, os, re, shutil, sys

GAME = r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules"
DONOR = os.path.join(GAME, "SandBox", "SceneObj", "sturgia_castle_siege_001")
SCENE_DIR = os.path.join(GAME, "RF_Map", "SceneObj")

INJECT = len(sys.argv) > 1 and sys.argv[1] == 'inject'
if INJECT:
    if len(sys.argv) < 3:
        print('uso: generate_scene.py inject <nome_da_cena_criada_no_editor>')
        sys.exit(1)
    OUT_NAME = sys.argv[2]
    STAGE = 3
else:
    STAGE = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    OUT_NAME = "rf_european_siege_001" if STAGE == 3 else f"rf_european_siege_t{STAGE}"
OUT = os.path.join(SCENE_DIR, OUT_NAME)

# ---------------------------------------------------------------- geometria base
# Modo inject: centro/alturas recalculados a partir do terreno da cena alvo (plano).
CX, CY, CZ = 560.5, 659.0, 81.0
GATE_Z_DROP = 4.0
WALL_LEN = 11.4

def recenters(cx, cy, cz):
    global CX, CY, CZ, VP
    CX, CY, CZ = cx, cy, cz
    VP = {k: brg(b, r) for k, (b, r) in V.items()}

def brg(deg, r, z=None, cx=None, cy=None):
    a = math.radians(deg)
    return ((cx if cx is not None else CX) + r * math.sin(a),
            (cy if cy is not None else CY) + r * math.cos(a),
            z if z is not None else CZ)

def yaw(deg_facing):
    d = ((deg_facing + 180.0) % 360.0) - 180.0
    return math.radians(d)

V = {
    'gate':  (187.0, 42.3),
    'sw':    (218.0, 44.0),
    'w':     (275.0, 46.0),
    'nw':    (330.0, 44.0),
    'keep':  ( 45.0, 40.0),
    'e':     ( 95.0, 45.0),
    'se':    (140.0, 44.0),
}
VP = {k: brg(b, r) for k, (b, r) in V.items()}

EDGES = [
    ('gate', 'sw',  None),
    ('sw',   'w',   'left'),
    ('w',    'nw',  None),
    ('nw',   'keep', None),
    ('keep', 'e',   None),
    ('e',    'se',  None),
    ('se',   'gate', 'right'),
]

NAVMESH_IDS = {
    ('left', 1):  [610, 611, 612, -1, 613, 614, 615, 616, 617],
    ('left', 2):  [620, 621, 622, -1, 623, 624, 625, 626, 627],
    ('left', 3):  [630, 631, 632, -1, 633, 634, 635, 636, 637],
    ('right', 1): [710, 711, 712, -1, 713, 714, 715, 716, 717],
    ('right', 2): [720, 721, 722, -1, 723, 724, 725, 726, 727],
    ('right', 3): [730, 731, 732, -1, 733, 734, 735, 736, 737],
}
LADDER_IDS = {('left', 1): 201, ('left', 2): 202, ('left', 3): 203,
              ('right', 1): 301, ('right', 2): 302, ('right', 3): 303}
GATE_IDS = {'outer': (501, 511), 'inner': (521, 531)}
WALL_VARS = ['_properGroundOutsideNavmeshID', '_properGroundInsideNavmeshID',
             '_underDebrisOutsideNavmeshID', '_underDebrisInsideNavmeshID',
             '_overDebrisOutsideNavmeshID', '_overDebrisInsideNavmeshID',
             '_underDebrisGenericNavmeshID', '_overDebrisGenericNavmeshID',
             '_onSolidWallGenericNavmeshID']

# ---------------------------------------------------------------- prefabs
PREFAB_FILES = {}   # nome -> caminho do arquivo que o define (top-level)

def load_prefab_universe():
    for mod in ('Native', 'SandBox'):
        d = os.path.join(GAME, mod, 'Prefabs')
        if not os.path.isdir(d):
            continue
        for f in os.listdir(d):
            if not f.endswith('.xml'):
                continue
            fp = os.path.join(d, f)
            try:
                txt = io.open(fp, encoding='utf-8', errors='replace').read()
            except OSError:
                continue
            for tag in re.findall(r'\n\t<game_entity([^>]*)>', txt):
                attrs = dict(re.findall(r'([a-z_]+)="([^"]*)"', tag))
                nm = attrs.get('name')
                if nm and nm not in PREFAB_FILES:
                    PREFAB_FILES[nm] = fp

load_prefab_universe()
MISSING = []

def check(prefab):
    if prefab not in PREFAB_FILES:
        MISSING.append(prefab)
    return prefab

_BLOCK_CACHE = {}

def extract_prefab_block(prefab):
    """Bloco <game_entity>...</game_entity> completo do prefab, do arquivo que o define."""
    if prefab in _BLOCK_CACHE:
        return _BLOCK_CACHE[prefab]
    fp = PREFAB_FILES.get(prefab)
    if not fp:
        MISSING.append(prefab)
        return None
    txt = io.open(fp, encoding='utf-8', errors='replace').read()
    for m in re.finditer(r'\n\t<game_entity([^>]*)>', txt):
        attrs = dict(re.findall(r'([a-z_]+)="([^"]*)"', m.group(1)))
        if attrs.get('name') != prefab:
            continue
        i = m.end()
        depth = 1
        while depth > 0:
            nxt_open = txt.find('<game_entity', i)
            nxt_close = txt.find('</game_entity>', i)
            if nxt_close < 0:
                return None
            if 0 <= nxt_open < nxt_close:
                depth += 1
                i = nxt_open + 12
            else:
                depth -= 1
                i = nxt_close + len('</game_entity>')
        block = txt[m.start() + 1:i]
        _BLOCK_CACHE[prefab] = (m.group(1), block)
        return _BLOCK_CACHE[prefab]
    MISSING.append(prefab)
    return None

# ---------------------------------------------------------------- emissao
OUT_ENTS = []
OUT_PATHS = []

def fmt(v):
    return f"{v:.3f}"

def ent(prefab=None, name=None, pos=(0, 0, 0), rot_z=0.0, tags=(), levels=(),
        scripts=None):
    """Referencia LEVE. Politica v2: name/tags proibidos em prefab (Breaking Prefab),
    exceto Camera_Instance (padrao comprovado no vanilla)."""
    if prefab and prefab != 'Camera_Instance':
        assert not name and not tags, f'name/tags em prefab ref nao-quebravel: {prefab}'
    a = []
    if name:
        a.append(f'name="{name}"')
    if prefab:
        a.append(f'prefab="{check(prefab)}"')
    parts = [f'\t\t<game_entity {" ".join(a)}>']
    if tags:
        parts.append('\t\t\t<tags>')
        for t in tags:
            parts.append(f'\t\t\t\t<tag name="{t}"/>')
        parts.append('\t\t\t</tags>')
    parts.append(f'\t\t\t<transform position="{fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])}" '
                 f'rotation_euler="0.000, 0.000, {fmt(rot_z)}"/>')
    if scripts:
        parts.append('\t\t\t<scripts>')
        for sname, svars in scripts:
            parts.append(f'\t\t\t\t<script name="{sname}">')
            parts.append('\t\t\t\t\t<variables>')
            for k, v in svars:
                parts.append(f'\t\t\t\t\t\t<variable name="{k}" value="{v}"/>')
            parts.append('\t\t\t\t\t</variables>')
            parts.append('\t\t\t\t</script>')
        parts.append('\t\t\t</scripts>')
    if levels:
        parts.append('\t\t\t<levels>')
        for l in levels:
            parts.append(f'\t\t\t\t<level name="{l}"/>')
        parts.append('\t\t\t</levels>')
    parts.append('\t\t</game_entity>')
    OUT_ENTS.append('\n'.join(parts))

def unpacked(prefab, name, pos, rot_z, tags=(), levels=(), script_name=None,
             overrides=None):
    """Entidade DESEMPACOTADA (old_prefab_name + bloco inline), como o vanilla faz com
    muralhas assaltaveis e portoes que precisam de tags/overrides por instancia."""
    got = extract_prefab_block(prefab)
    if not got:
        return
    top_attrs, block = got
    attrs = dict(re.findall(r'([a-z_]+)="([^"]*)"', top_attrs))
    attrs.pop('name', None)
    attrs.pop('old_prefab_name', None)
    rebuilt = f'<game_entity name="{name}" old_prefab_name="{prefab}"'
    for k, v in attrs.items():
        rebuilt += f' {k}="{v}"'
    rebuilt += '>'

    inner = block[block.index('>') + 1:block.rfind('</game_entity>')]
    # remove transform de topo pre-existente (profundidade 1)
    inner = re.sub(r'\n\t\t<transform[^>]*/>', '', inner, count=1)

    inject = ''
    if tags:
        inject += '\n\t\t<tags>'
        for t in tags:
            inject += f'\n\t\t\t<tag name="{t}"/>'
        inject += '\n\t\t</tags>'
    inject += (f'\n\t\t<transform position="{fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])}" '
               f'rotation_euler="0.000, 0.000, {fmt(rot_z)}"/>')

    body = inject + inner
    if script_name and overrides:
        # sobrescreve variaveis SO na primeira secao do script (a do topo da entidade)
        sm = re.search(r'(<script name="%s">.*?</script>)' % script_name, body, re.S)
        if sm:
            sec = sm.group(1)
            new_sec = sec
            for k, v in overrides:
                new_sec = re.sub(r'(<variable name="%s" value=")[^"]*(")' % re.escape(k),
                                 lambda mm: mm.group(1) + str(v) + mm.group(2), new_sec, count=1)
            body = body[:sm.start()] + new_sec + body[sm.end():]
        else:
            # prefab nao traz o script: acrescenta secao propria
            add = '\n\t\t<scripts>\n\t\t\t<script name="%s">\n\t\t\t\t<variables>' % script_name
            for k, v in overrides:
                add += f'\n\t\t\t\t\t<variable name="{k}" value="{v}"/>'
            add += '\n\t\t\t\t</variables>\n\t\t\t</script>\n\t\t</scripts>'
            body += add
    if levels:
        body += '\n\t\t<levels>'
        for l in levels:
            body += f'\n\t\t\t<level name="{l}"/>'
        body += '\n\t\t</levels>'

    OUT_ENTS.append('\t' + rebuilt + body + '\n\t</game_entity>')

def path(name, pts):
    p = [f'\t\t<path name="{name}" interpolation_type="hermite" '
         f'scene_upgrade_level_mask_="4294967295" season_mask_="15">',
         '\t\t\t<flags>', '\t\t\t\t<flag name="auto_tangent" value="true"/>', '\t\t\t</flags>',
         '\t\t\t<points>']
    for (x, y, z) in pts:
        p.append(f'\t\t\t\t<point position="{fmt(x)}, {fmt(y)}, {fmt(z)}" '
                 'scale="1.000, 1.000, 1.000" color="1.000, 1.000, 1.000, 1.000" '
                 'rotation="0.00000, 0.00000, 0.00000, 1.00000"/>')
    p.append('\t\t\t</points>')
    p.append('\t\t</path>')
    OUT_PATHS.append('\n'.join(p))

LV = lambda n: (f'level_{n}', 'siege', 'civilian')
ALL_LV = ('level_1', 'level_2', 'level_3', 'siege', 'civilian')

def edge_pieces(a, b):
    ax, ay, az = VP[a]; bx, by, bz = VP[b]
    dx, dy = bx - ax, by - ay
    length = math.hypot(dx, dy)
    bearing = math.degrees(math.atan2(dx, dy))
    n = max(1, int(round(length / WALL_LEN)))
    out = []
    for i in range(n):
        t = (i + 0.5) / n
        out.append(((ax + dx * t, ay + dy * t, az + (bz - az) * t), bearing, i, n))
    return out

# ---------------------------------------------------------------- construcao
def build():
    if STAGE == 1:
        return
    gz = CZ  # terreno plano no inject; alturas reais vem do sculpt posterior

    for lvl in (1, 2, 3):
        wall = f'european_castle_wall_a_l{lvl}'
        wall_broken = f'european_castle_wall_a_l{lvl}_broken'
        tower = f'european_castle_tower_round_a_l{lvl}'
        gatehouse = f'european_castle_gatehouse_a_l{lvl}'
        outer_gate_prefab = ('european_castle_gate_outer_l1' if lvl == 1 else
                             'european_castle_gate_outer_l2' if lvl == 2 else
                             'european_castle_gate_outer_l2_l3')

        for a, b, assault in EDGES:
            pieces = edge_pieces(a, b)
            mid = len(pieces) // 2
            for (pos, bearing, i, n) in pieces:
                facing = bearing + 90.0
                if assault and i == mid:
                    ids = NAVMESH_IDS[(assault, lvl)]
                    unpacked(wall_broken, f'rf_wall_{assault}_l{lvl}', pos, yaw(facing),
                             tags=(f'{assault}_wall_lvl{lvl}',), levels=LV(lvl),
                             script_name='WallSegment',
                             overrides=list(zip(WALL_VARS, ids)) + [('SideTag', assault)])
                else:
                    ent(prefab=wall, pos=pos, rot_z=yaw(facing), levels=LV(lvl))

        for vname in ('sw', 'w', 'nw', 'e', 'se'):
            b, r = V[vname]
            ent(prefab=tower, pos=VP[vname], rot_z=yaw(b), levels=LV(lvl))

        gb, _ = V['gate']
        ent(prefab=gatehouse, pos=VP['gate'], rot_z=yaw(gb), levels=LV(lvl))

        og = brg(gb, V['gate'][1] + 20.0, z=gz)
        unpacked(outer_gate_prefab, f'rf_outer_gate_l{lvl}', og, yaw(gb),
                 tags=('outer_gate',), levels=LV(lvl), script_name='CastleGate',
                 overrides=[('NavigationMeshId', GATE_IDS['outer'][0]),
                            ('NavigationMeshIdToDisableOnOpen', GATE_IDS['outer'][1])])
        unpacked(outer_gate_prefab, f'rf_inner_gate_l{lvl}', VP['gate'], yaw(gb),
                 tags=('inner_gate',), levels=LV(lvl), script_name='CastleGate',
                 overrides=[('NavigationMeshId', GATE_IDS['inner'][0]),
                            ('NavigationMeshIdToDisableOnOpen', GATE_IDS['inner'][1])])

        for side in ('left', 'right'):
            edge = ('sw', 'w') if side == 'left' else ('se', 'gate')
            pieces = edge_pieces(*edge)
            mpos, mbear, _, _ = pieces[len(pieces) // 2]
            out_dir = mbear + 90.0
            for k in (-1, 1):
                lp = (mpos[0] + 8.0 * k * math.sin(math.radians(mbear)),
                      mpos[1] + 8.0 * k * math.cos(math.radians(mbear)), mpos[2])
                lpp = (lp[0] + 2.0 * math.sin(math.radians(out_dir)),
                       lp[1] + 2.0 * math.cos(math.radians(out_dir)), lp[2])
                ent(prefab='siege_ladder_8m_spawner',
                    pos=lpp, rot_z=yaw(out_dir + 180.0), levels=LV(lvl),
                    scripts=[('SiegeLadderSpawner', [
                        ('SideTag', side),
                        ('TargetWallSegmentTag', f'{side}_wall_lvl{lvl}'),
                        ('OnWallNavMeshId', str(LADDER_IDS[(side, lvl)])),
                        ('TacticalPositionWidth', '4.000'),
                        ('BarrierTagToRemove', f'{side}_ladder_barrier_{"a" if k < 0 else "b"}'),
                        ('IndestructibleMerlonsTag', f'merlon_solid_{side}')])])
            tp = (mpos[0] + 6.0 * math.sin(math.radians(out_dir)),
                  mpos[1] + 6.0 * math.cos(math.radians(out_dir)), mpos[2])
            pname = f'siege_tower_path_{side}' + ('' if lvl == 3 else f'_lvl{lvl}')
            ent(prefab='siege_tower_9m_spawner',
                pos=tp, rot_z=yaw(out_dir + 180.0), levels=LV(lvl),
                scripts=[('SiegeTowerSpawner', [
                    ('SideTag', side),
                    ('TargetWallSegmentTag', f'{side}_wall_lvl{lvl}'),
                    ('PathEntityName', pname),
                    ('RampRotationDegree', '90.000'),
                    ('BarrierLength', '3.000'),
                    ('BarrierTagToRemove', f'tower_barrier_lvl{lvl}_{side}')])])
            far = (mpos[0] + 90.0 * math.sin(math.radians(out_dir)),
                   mpos[1] + 90.0 * math.cos(math.radians(out_dir)), gz)
            mid_pt = (mpos[0] + 45.0 * math.sin(math.radians(out_dir)),
                      mpos[1] + 45.0 * math.cos(math.radians(out_dir)), gz)
            path(pname, [far, mid_pt, tp])

        ram_path_name = 'ram_path' if lvl == 3 else f'ram_path_lvl{lvl}'
        ram_start = brg(V['gate'][0], V['gate'][1] + 80.0, z=gz)
        ent(prefab='batteringram_a_spawner',
            pos=ram_start, rot_z=yaw(V['gate'][0] + 180.0), levels=LV(lvl),
            scripts=[('BatteringRamSpawner', [
                ('SideTag', 'middle'), ('GateTag', 'outer_gate'),
                ('PathEntityName', ram_path_name)])])
        path(ram_path_name, [ram_start,
                             brg(V['gate'][0], V['gate'][1] + 45.0, z=gz),
                             brg(V['gate'][0], V['gate'][1] + 22.0, z=gz)])

    if STAGE == 2:
        return

    kb, _ = V['keep']
    ent(prefab='european_castle_keep', pos=VP['keep'], rot_z=yaw(kb + 180.0),
        levels=ALL_LV)

    # muralha interna defensavel
    iw_a = brg(340.0, 20.0)
    iw_b = brg(165.0, 24.0)
    dx, dy = iw_b[0] - iw_a[0], iw_b[1] - iw_a[1]
    ib = math.degrees(math.atan2(dx, dy))
    n = max(1, int(round(math.hypot(dx, dy) / WALL_LEN)))
    for i in range(n):
        t = (i + 0.5) / n
        pos = (iw_a[0] + dx * t, iw_a[1] + dy * t, CZ)
        prefab = 'european_castle_wall_gate_l2' if i == n // 2 else 'european_castle_wall_a_l2'
        ent(prefab=prefab, pos=pos, rot_z=yaw(ib + 90.0), levels=ALL_LV)
        if i != n // 2:
            ent(prefab='strategic_archer_point', pos=(pos[0], pos[1], CZ + 5.0),
                rot_z=yaw(ib + 90.0), levels=ALL_LV,
                scripts=[('StrategicArea', [
                    ('_width', '1.000'), ('_side', 'Defender'), ('_depth', '1.000'),
                    ('_distanceToCheck', '40.000'), ('_ignoreHeight', 'true'),
                    ('_disableShimmy', 'false'), ('NavMeshPrefabName', '')])])

    # arqueiros
    for i, (vname, extra_b) in enumerate([('sw', 0), ('w', 0), ('nw', 0),
                                          ('e', 0), ('se', 0), ('gate', 12)]):
        b, r = V[vname]
        p = brg(b + extra_b, r - 3.0, z=CZ + 8.0)
        ent(prefab='defender_archer_position', pos=p, rot_z=yaw(b), levels=ALL_LV)
        for j in (-1, 1):
            sp = brg(b + extra_b + 6 * j, r - 2.0, z=CZ + 8.0)
            ent(prefab='strategic_archer_point', pos=sp, rot_z=yaw(b), levels=ALL_LV,
                scripts=[('StrategicArea', [
                    ('_width', '1.000'), ('_side', 'Defender'), ('_depth', '1.000'),
                    ('_distanceToCheck', '40.000'), ('_ignoreHeight', 'true'),
                    ('_disableShimmy', 'false'), ('NavMeshPrefabName', '')])])
    for i in range(2):
        p = brg(187.0 + 25 * (i * 2 - 1), 95.0, z=CZ)
        ent(prefab='attacker_archer_position', pos=p, rot_z=yaw(7.0), levels=ALL_LV)

    # deployment points (prefab do doador; sem name/tags)
    for i in range(4):
        p = brg(160.0 + i * 18.0, 100.0, z=CZ)
        ent(prefab='siege_deployment_placeholder_part_b', pos=p, levels=ALL_LV,
            scripts=[('DeploymentPoint', [('Side', 'Attacker'), ('Radius', '6.000')])])
        q = brg(30.0 + i * 80.0, 20.0, z=CZ)
        ent(prefab='siege_deployment_placeholder_part_b', pos=q, levels=ALL_LV,
            scripts=[('DeploymentPoint', [('Side', 'Defender'), ('Radius', '6.000')])])

    # cameras (name+tags comprovados no vanilla para Camera_Instance)
    ent(prefab='Camera_Instance', name='rf_cam_attacker', pos=brg(187.0, 120.0, z=CZ + 30.0),
        rot_z=yaw(7.0), tags=('strategycameraattacker',), levels=ALL_LV)
    ent(prefab='Camera_Instance', name='rf_cam_defender', pos=(CX, CY, CZ + 35.0),
        rot_z=yaw(187.0), tags=('strategycameradefender',), levels=ALL_LV)

    # engenhos estaticos
    for i, vname in enumerate(('sw', 'se')):
        b, r = V[vname]
        ent(prefab='mangonel_a_spawner', pos=brg(b, r - 6.0, z=CZ), rot_z=yaw(b),
            levels=ALL_LV)
    for i, vname in enumerate(('w', 'e')):
        b, r = V[vname]
        ent(prefab='ballista_a_spawner', pos=brg(b, r - 6.0, z=CZ), rot_z=yaw(b),
            levels=ALL_LV)
    for i in range(2):
        ent(prefab='trebuchet_a_spawner', pos=brg(170.0 + i * 34.0, 110.0, z=CZ),
            rot_z=yaw(7.0), levels=ALL_LV)
        ent(prefab='mangonel_a_spawner', pos=brg(176.0 + i * 22.0, 104.0, z=CZ),
            rot_z=yaw(7.0), levels=ALL_LV)
    for side in ('left', 'right'):
        edge = ('sw', 'w') if side == 'left' else ('se', 'gate')
        pieces = edge_pieces(*edge)
        mpos, mbear, _, _ = pieces[len(pieces) // 2]
        ent(prefab='throwable_rock_pile', pos=(mpos[0], mpos[1], mpos[2] + 8.0),
            rot_z=yaw(mbear + 90.0), levels=ALL_LV)
    # NOTA: sp_battle_set, boundaries e flee lines ficam para a fase de editor —
    # o doador vanilla funciona sem eles (fallback de runtime) e as tags por
    # instancia deles esbarram no Breaking Prefab.

# ---------------------------------------------------------------- montagem
def main():
    if INJECT:
        target = os.path.join(OUT, 'scene.xscene')
        if not os.path.isfile(target):
            print(f'Cena alvo nao existe: {target}')
            print('Crie a cena no editor primeiro (New Scene, terreno habilitado), salve e feche.')
            sys.exit(1)
        xml = io.open(target, encoding='utf-8', errors='replace').read()
        # centro do terreno da cena alvo
        tm = re.search(r'<terrain[^>]*node_dimension_x="(\d+)"[^>]*node_dimension_y="(\d+)"[^>]*node_size="([0-9.]+)"', xml)
        hm = re.search(r'min_height="([0-9.\-]+)" max_height="([0-9.\-]+)"', xml)
        # sem acesso ao heightmap binario, o melhor palpite de chao e o meio da faixa;
        # o ajuste fino e um snap-to-ground do editor
        zb = (float(hm.group(1)) + float(hm.group(2))) / 2.0 if hm else 0.0
        if tm:
            nx, ny, ns = int(tm.group(1)), int(tm.group(2)), float(tm.group(3))
            recenters(nx * ns / 2.0, ny * ns / 2.0 + 20.0, zb)
        else:
            recenters(200.0, 220.0, zb)

        build()
        if MISSING:
            print('CANARIO: prefabs ausentes ->', sorted(set(MISSING)))
            sys.exit(1)

        ents = '\n'.join(OUT_ENTS)
        if '<entities>' in xml:
            # injeta ao FINAL das entidades existentes (preserva o que o editor criou)
            xml = xml.replace('</entities>', ents + '\n\t</entities>', 1)
        else:
            xml = xml.replace('</scene>', '\t<entities>\n' + ents + '\n\t</entities>\n</scene>')
        pth = '\n'.join(OUT_PATHS)
        if '<Paths>' in xml:
            xml = xml.replace('</Paths>', pth + '\n\t</Paths>', 1)
        else:
            xml = xml.replace('</scene>', '\t<Paths>\n' + pth + '\n\t</Paths>\n</scene>')

        shutil.copy2(target, target + '.bak_pre_inject')
        io.open(target, 'w', encoding='utf-8').write(xml)
        print(f'INJETADO em {target}')
        print(f'centro usado: ({fmt(CX)}, {fmt(CY)}, {fmt(CZ)}) | entidades: {len(OUT_ENTS)} | paths: {len(OUT_PATHS)}')
        print('backup: scene.xscene.bak_pre_inject')
        return

    # modo legado (doador — terreno nao renderiza no editor; util so p/ validar XML)
    donor = io.open(os.path.join(DONOR, 'scene.xscene'), encoding='utf-8', errors='replace').read()
    header_end = donor.find('</environment_properties>') + len('</environment_properties>')
    header = donor[:header_end].replace('name="sturgia_castle_siege_001"', f'name="{OUT_NAME}"', 1)
    t_a = donor.find('<terrain ')
    t_b = donor.find('</terrain>') + len('</terrain>')
    terrain = donor[t_a:t_b]

    build()
    if MISSING:
        print('CANARIO: prefabs ausentes ->', sorted(set(MISSING)))
        sys.exit(1)

    xml = (header + '\n\t<entities>\n' + '\n'.join(OUT_ENTS) + '\n\t</entities>\n\t'
           + terrain + '\n\t<editor_data author="rf_generator" comment="Scripts/SiegeSceneGen"/>\n'
           '\t<camera_data near_plane="0.100" far_plane="1500000.000"/>\n'
           '\t<Paths>\n' + '\n'.join(OUT_PATHS) + '\n\t</Paths>\n</scene>\n')

    os.makedirs(OUT, exist_ok=True)
    io.open(os.path.join(OUT, 'scene.xscene'), 'w', encoding='utf-8').write(xml)
    for f in ('terrain.bin', 'flora.bin', 'navmesh.bin', 'atmosphere.xml'):
        shutil.copy2(os.path.join(DONOR, f), os.path.join(OUT, f))
    print(f'OK: {OUT} | entidades: {len(OUT_ENTS)} | paths: {len(OUT_PATHS)}')

if __name__ == '__main__':
    main()
