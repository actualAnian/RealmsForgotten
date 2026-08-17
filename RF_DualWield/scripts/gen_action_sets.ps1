# Gera RF_DualWield/_Module/ModuleData/action_sets.xml a partir de action_types.xml.
# Regra: TODA action declarada em action_types.xml precisa de um <action type=... animation=.../>
# dentro de as_human_warrior, senao o engine resolve animation index = -1 e crasha nativamente.

$proj   = "C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_DualWield"
$types  = Join-Path $proj "_Module\ModuleData\action_types.xml"
$assets = Join-Path $proj "_Module\Assets"
$out    = Join-Path $proj "_Module\ModuleData\action_sets.xml"

# --- clipes de animacao proprios do modulo (nome = arquivo sem sufixo _anm.tpac) ---
$ownClips = @{}
Get-ChildItem $assets -Filter *_anm.tpac | ForEach-Object {
    $ownClips[($_.BaseName -replace '_anm$','')] = $true
}

# --- mapa action -> animation ---
# Fonte "D" = clipe proprio do modulo (.tpac). Fonte "V" = animacao vanilla ja usada por as_human_warrior.
$map = [ordered]@{}

# ---------- THRUST (attack_down) ----------
$thrust = [ordered]@{
    'act_dual_ready_thrust_1h'                    = 'ready_dual_thrust_1h'                   # D
    'act_dual_quick_release_thrust_1h'            = 'quick_release_dual_thrust_1h'            # D
    'act_dual_quick_release_thrust_1h_balanced'   = 'quick_release_dual_thrust_1h_balanced'   # D
    'act_dual_release_thrust_1h'                  = 'release_dual_thrust_1h'                  # D
    'act_dual_release_thrust_1h_balanced'         = 'release_dual_thrust_1h_balanced'         # D
    'act_dual_quick_blocked_thrust_1h'            = 'quick_dual_blocked_thrust_1h'            # D
    'act_dual_quick_blocked_thrust_1h_balanced'   = 'quick_dual_blocked_thrust_1h_balanced'   # D
    'act_dual_blocked_thrust_1h'                  = 'quick_dual_blocked_thrust_1h'            # D (reuso)
    'act_dual_blocked_thrust_1h_balanced'         = 'quick_dual_blocked_thrust_1h_balanced'   # D (reuso)
    'act_dual_stuck_thrust_1h'                    = 'stuck_thrust_1h'                         # V
    'act_dual_stuck_quick_thrust_1h'              = 'stuck_quick_thrust_1h'                   # V
}
foreach ($k in $thrust.Keys) { $map[$k] = $thrust[$k] }
# left_stance: nao existe clipe dual left_stance de ataque -> reusa o mesmo clipe.
$map['act_dual_ready_thrust_1h_left_stance']                  = 'ready_dual_thrust_1h'
$map['act_dual_quick_release_thrust_1h_left_stance']          = 'quick_release_dual_thrust_1h'
$map['act_dual_quick_release_thrust_1h_balanced_left_stance'] = 'quick_release_dual_thrust_1h_balanced'
$map['act_dual_release_thrust_1h_left_stance']                = 'release_dual_thrust_1h'
$map['act_dual_release_thrust_1h_balanced_left_stance']       = 'release_dual_thrust_1h_balanced'
$map['act_dual_quick_blocked_thrust_1h_left_stance']          = 'quick_dual_blocked_thrust_1h'
$map['act_dual_quick_blocked_thrust_1h_balanced_left_stance'] = 'quick_dual_blocked_thrust_1h_balanced'
$map['act_dual_blocked_thrust_1h_left_stance']                = 'quick_dual_blocked_thrust_1h'
$map['act_dual_blocked_thrust_1h_balanced_left_stance']       = 'quick_dual_blocked_thrust_1h_balanced'
$map['act_dual_stuck_thrust_1h_left_stance']                  = 'stuck_thrust_1h_left_stance'
$map['act_dual_stuck_quick_thrust_1h_left_stance']            = 'stuck_quick_thrust_1h_left_stance'

# ---------- SLASH RIGHT ----------
$map['act_dual_ready_slashright_1h']                       = 'ready_slashright_1h'                       # V
$map['act_dual_ready_from_up_slashright_1h']               = 'ready_from_up_slashright_1h'               # V
$map['act_dual_ready_from_up_slashright_1h_unbalanced']    = 'ready_from_up_slashright_1h_unbalanced'    # V
$map['act_dual_ready_from_right_slashright_1h']            = 'ready_from_right_slashright_1h'            # V
$map['act_dual_ready_from_right_slashright_1h_unbalanced'] = 'ready_from_right_slashright_1h_unbalanced' # V
$map['act_dual_quick_release_slashright_1h']               = 'quick_release_dual_slashright_1h'          # D
$map['act_dual_quick_release_slashright_1h_balanced']      = 'quick_release_dual_slashright_1h_balanced' # D
$map['act_dual_release_slashright_1h']                     = 'quick_release_dual2_slashright_1h'         # D
$map['act_dual_release_slashright_1h_balanced']            = 'quick_release_dual2_slashright_1h_balanced'# D
$map['act_dual_quick_blocked_slashright_1h']               = 'quick_dual_blocked_slashright_1h'          # D
$map['act_dual_quick_blocked_slashright_1h_balanced']      = 'quick_dual_blocked_slashright_1h_balanced' # D
$map['act_dual_blocked_slashright_1h']                     = 'quick_dual_blocked_slashright_1h'          # D
$map['act_dual_blocked_slashright_1h_balanced']            = 'quick_dual_blocked_slashright_1h_balanced' # D

$map['act_dual_ready_slashright_1h_left_stance']                       = 'ready_slashright_1h_left_stance'
$map['act_dual_ready_from_up_slashright_1h_left_stance']               = 'ready_from_up_slashright_1h_left_stance'
$map['act_dual_ready_from_up_slashright_1h_unbalanced_left_stance']    = 'ready_from_up_slashright_1h_left_stance_unbalanced'
$map['act_dual_ready_from_right_slashright_1h_left_stance']            = 'ready_from_right_slashright_1h_left_stance'
$map['act_dual_ready_from_right_slashright_1h_unbalanced_left_stance'] = 'ready_from_right_slashright_1h_left_stance_unbalanced'
$map['act_dual_quick_release_slashright_1h_left_stance']               = 'quick_release_dual_slashright_1h'
$map['act_dual_quick_release_slashright_1h_balanced_left_stance']      = 'quick_release_dual_slashright_1h_balanced'
$map['act_dual_release_slashright_1h_left_stance']                     = 'quick_release_dual2_slashright_1h'
$map['act_dual_release_slashright_1h_balanced_left_stance']            = 'quick_release_dual2_slashright_1h_balanced'
$map['act_dual_quick_blocked_slashright_1h_left_stance']               = 'quick_dual_blocked_slashright_1h'
$map['act_dual_quick_blocked_slashright_1h_balanced_left_stance']      = 'quick_dual_blocked_slashright_1h_balanced'
$map['act_dual_blocked_slashright_1h_left_stance']                     = 'quick_dual_blocked_slashright_1h'
$map['act_dual_blocked_slashright_1h_balanced_left_stance']            = 'quick_dual_blocked_slashright_1h_balanced'

# ---------- SLASH LEFT ----------
$map['act_dual_ready_slashleft_1h']                      = 'ready_dual_slashleft_1h'                   # D
$map['act_dual_ready_from_up_slashleft_1h']              = 'ready_from_up_slashleft_1h'                # V
$map['act_dual_ready_from_up_slashleft_1h_unbalanced']   = 'ready_from_up_slashleft_1h_unbalanced'     # V
$map['act_dual_ready_from_left_slashleft_1h']            = 'ready_from_left_slashleft_1h'              # V
$map['act_dual_ready_from_left_slashleft_1h_unbalanced'] = 'ready_from_left_slashleft_1h_unbalanced'   # V
$map['act_dual_quick_release_slashleft_1h']              = 'quick_release_dual_slashleft_1h'           # D
$map['act_dual_quick_release_slashleft_1h_balanced']     = 'quick_release_dual_slashleft_1h_balanced'  # D
$map['act_dual_release_slashleft_1h']                    = 'quick_release_dual_slashleft_1h'           # D
$map['act_dual_release_slashleft_1h_balanced']           = 'quick_release_dual_slashleft_1h_balanced'  # D
$map['act_dual_quick_blocked_slashleft_1h']              = 'quick_dual_blocked_slashleft_1h'           # D
$map['act_dual_quick_blocked_slashleft_1h_balanced']     = 'quick_dual_blocked_slashleft_1h_balanced'  # D
$map['act_dual_blocked_slashleft_1h']                    = 'quick_dual_blocked_slashleft_1h'           # D
$map['act_dual_blocked_slashleft_1h_balanced']           = 'quick_dual_blocked_slashleft_1h_balanced'  # D

$map['act_dual_ready_slashleft_1h_left_stance']                      = 'ready_slashleft_1h_left_stance'
$map['act_dual_ready_from_up_slashleft_1h_left_stance']              = 'ready_from_up_slashleft_1h_left_stance'
$map['act_dual_ready_from_up_slashleft_1h_unbalanced_left_stance']   = 'ready_from_up_slashleft_1h_left_stance_unbalanced'
$map['act_dual_ready_from_left_slashleft_1h_left_stance']            = 'ready_from_left_slashleft_1h_left_stance'
$map['act_dual_ready_from_left_slashleft_1h_unbalanced_left_stance'] = 'ready_from_left_slashleft_1h_left_stance_unbalanced'
$map['act_dual_quick_release_slashleft_1h_left_stance']              = 'quick_release_dual_slashleft_1h'
$map['act_dual_quick_release_slashleft_1h_balanced_left_stance']     = 'quick_release_dual_slashleft_1h_balanced'
$map['act_dual_release_slashleft_1h_left_stance']                    = 'quick_release_dual_slashleft_1h'
$map['act_dual_release_slashleft_1h_balanced_left_stance']           = 'quick_release_dual_slashleft_1h_balanced'
$map['act_dual_quick_blocked_slashleft_1h_left_stance']              = 'quick_dual_blocked_slashleft_1h'
$map['act_dual_quick_blocked_slashleft_1h_balanced_left_stance']     = 'quick_dual_blocked_slashleft_1h_balanced'
$map['act_dual_blocked_slashleft_1h_left_stance']                    = 'quick_dual_blocked_slashleft_1h'
$map['act_dual_blocked_slashleft_1h_balanced_left_stance']           = 'quick_dual_blocked_slashleft_1h_balanced'

# ---------- LOCOMOCAO (movement_sets.xml) ----------
$map['act_walk_idle_1h_with_d_shld']                 = 'dual_stand_1h'                                    # D
$map['act_walk_forward_1h_with_d_shld']              = 'dual_walk_forward_1h'                             # D
$map['act_walk_idle_1h_with_d_shld_left_stance']     = 'dual_stand_1h_left_stance'                        # D
$map['act_walk_forward_1h_with_d_shld_left_stance']  = 'dual_walk_forward_1h_left_stance'                 # D
$map['act_run_idle_1h_with_d_shld']                  = 'dual_stand_1h'                                    # D
$map['act_run_forward_1h_with_d_shld']               = 'dual_run_forward_1h_with_hand_shield'             # D
$map['act_run_idle_1h_with_d_shld_left_stance']      = 'dual_stand_1h_left_stance'                        # D
$map['act_run_forward_1h_with_d_shld_left_stance']   = 'dual_run_forward_1h_with_hand_shield_left_stance' # D

# --- le as actions declaradas ---
$declared = [regex]::Matches((Get-Content $types -Raw), '<action\s+name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
Write-Output ("action_types declaradas: " + $declared.Count)

$missing = $declared | Where-Object { -not $map.Contains($_) }
if ($missing.Count -gt 0) { Write-Output "ERRO - actions sem mapeamento:"; $missing | ForEach-Object { Write-Output ("   " + $_) }; exit 1 }
$extra = $map.Keys | Where-Object { $declared -notcontains $_ }
if ($extra.Count -gt 0) { Write-Output "ERRO - mapeamentos sem action_type:"; $extra | ForEach-Object { Write-Output ("   " + $_) }; exit 1 }

# --- valida clipes proprios ---
$ownUsed = $map.Values | Sort-Object -Unique | Where-Object { $_ -match '(^dual_|dual)' -and $_ -notmatch '^(ready_|release_|stuck_|quick_release_slash|ready_from)' }
foreach ($clip in ($map.Values | Sort-Object -Unique)) {
    $isOwn = $ownClips.ContainsKey($clip)
    if (-not $isOwn -and $clip -match 'dual') {
        Write-Output ("ERRO - clipe 'dual' referenciado sem .tpac correspondente: " + $clip); exit 1
    }
}
$ownCount = ($map.Values | Sort-Object -Unique | Where-Object { $ownClips.ContainsKey($_) }).Count
$vanCount = ($map.Values | Sort-Object -Unique).Count - $ownCount
Write-Output ("clipes usados: proprios=" + $ownCount + " / vanilla=" + $vanCount)
$unusedOwn = $ownClips.Keys | Where-Object { $map.Values -notcontains $_ }
if ($unusedOwn) { Write-Output ("clipes .tpac nao usados: " + ($unusedOwn -join ', ')) }

# --- emite XML ---
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<!--')
[void]$sb.AppendLine('  RF_DualWield / action_sets.xml')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('  Este arquivo MESCLA actions em as_human_warrior (o loader nativo faz merge por id;')
[void]$sb.AppendLine('  o mesmo padrao ja e usado por RealmsForgotten/ModuleData/action_sets.xml).')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('  REGRA CRITICA: toda action declarada em action_types.xml deste modulo PRECISA de um')
[void]$sb.AppendLine('  binding aqui. Sem binding, MBActionSet.GetAnimationIndexOfAction devolve -1 e o engine')
[void]$sb.AppendLine('  crasha nativamente (CTD sem excecao gerenciada). Evidencia no rgl_log:')
[void]$sb.AppendLine('     "as_human_warrior does not contain act_dual_quick_release_thrust_1h"  /  "5013 -1 6218"')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('  GERADO por scripts/gen_action_sets.ps1 - nao editar a mao; editar o mapa do script.')
[void]$sb.AppendLine('-->')
[void]$sb.AppendLine('<action_sets>')
[void]$sb.AppendLine('  <action_set id="as_human_warrior" skeleton="human_skeleton" movement_system="bipedal">')

function Emit([string]$title, [string[]]$keys) {
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('    <!-- ' + $title + ' -->')
    foreach ($k in $keys) {
        [void]$sb.AppendLine('    <action type="' + $k + '" animation="' + $map[$k] + '" />')
    }
}
Emit 'thrust (attack_down)'                 ($map.Keys | Where-Object { $_ -match 'thrust' })
Emit 'slash right (attack_right)'           ($map.Keys | Where-Object { $_ -match 'slashright' })
Emit 'slash left (attack_left)'             ($map.Keys | Where-Object { $_ -match 'slashleft' })
Emit 'locomocao com arma na mao esquerda'   ($map.Keys | Where-Object { $_ -match '_d_shld' })

[void]$sb.AppendLine('')
[void]$sb.AppendLine('  </action_set>')
[void]$sb.AppendLine('</action_sets>')

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("escrito: " + $out)

# --- emite a tabela C# usada pela validacao em runtime (mesma fonte, nunca divergem) ---
$cs = New-Object System.Text.StringBuilder
[void]$cs.AppendLine('// <auto-generated />')
[void]$cs.AppendLine('// GERADO por scripts/gen_action_sets.ps1 a partir de _Module/ModuleData/action_types.xml.')
[void]$cs.AppendLine('// Nao editar a mao. Esta lista tem de conter EXATAMENTE as mesmas actions que')
[void]$cs.AppendLine('// action_sets.xml liga a animacoes; e o que a validacao de missao confere antes de')
[void]$cs.AppendLine('// deixar o dual wield ativo.')
[void]$cs.AppendLine('namespace RF_DualWield')
[void]$cs.AppendLine('{')
[void]$cs.AppendLine('    internal static class DualWieldActionTable')
[void]$cs.AppendLine('    {')
[void]$cs.AppendLine('        internal static readonly string[] ActionNames =')
[void]$cs.AppendLine('        {')
foreach ($k in $map.Keys) { [void]$cs.AppendLine('            "' + $k + '",') }
[void]$cs.AppendLine('        };')
[void]$cs.AppendLine('    }')
[void]$cs.AppendLine('}')
$csOut = Join-Path $proj "DualWieldActionTable.g.cs"
[System.IO.File]::WriteAllText($csOut, $cs.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("escrito: " + $csOut)
