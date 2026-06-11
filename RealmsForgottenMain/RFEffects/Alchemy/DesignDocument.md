# Bannerlord Alchemy System

## Design Philosophy

* Bombs create persistent fields/surfaces.
* Fields react with:

  * Agents
  * Projectiles
  * Other fields
  * Formations
  * Captains
  * Corpses
  * Siege equipment
* A DOS2 inspired system with a focus on Bannerlord mechanics

## Possible Field Properties

```csharp
[Flags]
enum AlchemyProperties
{
    None = 0,
    Flammable = 1,
    Wet = 2,
    Toxic = 4,
    Conductive = 8,
    Explosive = 16,
    Oily = 32,
    Frozen = 64,
    Corrosive = 128,
    Magical = 256,
    Smoke = 512,
    Sticky = 1024
}
```

## Possible Events

```csharp
void OnCreated();
void OnDestroyed();
void OnEntered(Agent agent);
void OnLeft(Agent agent);
void OnProjectileEntered(Mission.Missile missile);
void OnProjectileLeft(Mission.Missile missile);
void OnBombCollision(IAlchemicalBomb otherField);
void OnFieldCreatedInside(IAlchemicalBomb otherField);
void OnTick(float dt);
void OnDamageReceived(float damage, DamageTypes type);
void OnElementalInteraction(IAlchemicalBomb otherBomb, AlchemyProperties other interaction)
## example interations
void OnIgnited();
void OnExtinguished();
void OnFrozen();
void OnMelted();
void OnWet();
void OnLightningStrike();
##
void OnAgentDiedInside(Agent agent);
void OnMountedAgentEntered(Agent rider);
void OnCaptainEntered(Agent captain);
void OnFormationEntered(Formation formation);
void OnFormationBroken(Formation formation);
void OnMoraleBroken(Agent agent);
void OnShieldBlockInside(Agent defender);
void OnMeleeHitInside(Agent attacker, Agent victim);
void OnRangedHitInside(Agent attacker, Agent victim);
void OnHorseChargeInside(Agent horse);
void OnSiegeEngineEntered(GameEntity siegeEngine);
void OnBannerEntered(Agent bannerBearer);
void OnCorpseCreated(Agent deadAgent);
void OnFieldMerged(AlchemyField otherField);
void OnFieldSplit();
void OnFieldExpired();
```

## Interesting editable AgentDrivenProperties
```
SwingSpeedMultiplier
ReloadSpeed

WeaponInaccuracy

ArmorEncumbrance

ArmorPenetrationMultiplierCrossbow
ArmorPenetrationMultiplierBow

ArmorHead
ArmorTorso
ArmorLegs
ArmorArms   

MaxSpeedMultiplier
CombatMaxSpeedMultiplier

MountManeuver
MountSpeed
MountChargeDamage
MountDifficulty
```

# Core Bombs

## Cleansing Bomb
* Removes all effects from other bombs in radius
* Weakens undead troops

## Smoke Bomb

* Prevents agents inside from using ranged weapons
* Missiles entered will have their trajectory slighly changed (to simulate agents not seeing enemies on other side clearly).
* Reduced spotting distance .
* Fire burns smoke away.
* Can become toxic smoke.

## Heat Bomb

* +20% attack speed.
* Ignites oil, tar and gunpowder.
* missiles passing through fire get fire effect and +1 morale damage on hit

## Oil Bomb

* Reduced movement.
* Chance to stumble.
* Ignites into large fire field.

## Frost Bomb

* -20% attack speed.
* Projectiles passing through drop to the ground

## Acid Bomb

* Corrodes armor.
* Damages shields.
* Projectiles passing pierce armor or extra damage to shields
* Fire creates toxic fumes.

## Poison Bomb

* Toxic cloud.
* Damage over time.
* Reduced stamina.
* Projectiles passing deal 1 dmg per second for 10 seconds

## Flash Bomb

* Blinds units.
* AI loses target.
* Reduces ranged accuracy.

## Quicksilver Bomb

* Increased movement speed.
* Reduced melee accuracy.
* Lightning interactions.

# Formation Warfare Bombs

## Panic Dust

* Morale damage.
* Units inside are detached from formation.

## Berserker Dust

* Units much less likely to defend, they constantly attack.

## Stalwart Dust

* Units much less likely to attack, they constantly defend.
* armor rating increased by 100%

## Banner Burn

* Removes captain and banner bonuses.

### Reactions

* Dead captain inside -> panic effect.

# Anti-Cavalry Bombs

## Horsebane Powder

* Affects horses only.
* Reduced speed and charge damage.

### Reactions

* Heat -> horses are forced to move in a random direction 10 distance away.

* Freeze -> 0 speed for horses.


# Siege Bombs

## Mason's Bane

* Strong against gates and walls.
* knockback against troops.

### Reactions

* Fire -> structural cracking.
* Frost -> brittleness.

## Rope Rot

* pushed siege ladders down (plenty to do to get it to work)
* Damages siege engines.

# Fantasy Bombs

## Echo Crystal

* Forces AI to move 10 distance away

## Moon Silver Bomb

* Anti-magic field.
* Suppresses summons.


# Corpse-Based Alchemy

## Carrion Mist (change name)

* Grows stronger whenever units die inside.
* Creates a wight unit with health of all units that died on duration end

## (give it a cool name)
* Heals units of opposite side on troop died
* Consumes corpses.

## Bone Bloom
* Creates bone spikes and obstacles.

## Create

# Commander-Focused Alchemy

## Oathfire

* Friendly morale aura.
* Rally point.

### Reactions

* Captain inside -> swing speed, reload speed

## King's Ash

* Every unit that passed through fights to the death.

## Restore Ammo
* restores ammunition to units inside


#
#
#
#
#
#
# If doable ideas

## Oil Trail

* Ignitable path.

## Poison Cone

* Directional gas cloud.

## Spike Patch

* Anti-cavalry trap.

## Rune Trap

* Single-trigger magical trap.

## Rune Posts

* Two connected deployables.
* Trigger effect when crossed.