using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Patches
{
    public class CharacterRacialMix
    {
        [SaveableField(1)]
        public CharacterObject Character;
        [SaveableField(2)]
        public Dictionary<int, double> Races = new Dictionary<int, double>();

        public CharacterRacialMix(CharacterObject character)
        {
            Character = character;
        }

        // Updates the character's race to the one with the highest percentage.
        public void SetCharacterRace()
        {
            int chosenRace = 0;
            double bestEffectiveValue = double.MinValue;
            // Iterate through all races in the mix.
            foreach (var pair in Races)
            {
                double effectiveValue = pair.Value;
                // If the race is human, reduce its effective value (e.g., by 20%).
                if (pair.Key == FaceGen.GetRaceOrDefault("human"))
                {
                    effectiveValue *= 0.8;
                }
                // You could add other conditions for other default races if desired.

                if (effectiveValue > bestEffectiveValue)
                {
                    bestEffectiveValue = effectiveValue;
                    chosenRace = pair.Key;
                }
            }
    // Set the character's race to the chosen one.
    ((BasicCharacterObject)Character).Race = chosenRace;
        }

        // Retrieves the race mix for a character, or null if not created.
        public static CharacterRacialMix GetForCharacter(CharacterObject character)
        {
            try
            {
                return RacialMixingBehavior.Instance.AllCharacterRacialMixes[character];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        // Creates a race mix for an existing hero if not already present.
        public static void CreateForExisting(Hero hero)
        {
            CharacterObject characterObject = hero.CharacterObject;
            if (GetForCharacter(characterObject) != null)
                return;
            Create(characterObject);
        }

        // Handles race mixing for a newborn by averaging parental race values.
        public static void CreateForNewborn(Hero hero)
        {
            Hero father = hero.Father;
            Hero mother = hero.Mother;
            CharacterRacialMix mixFather = GetForCharacter(father.CharacterObject);
            CharacterRacialMix mixMother = GetForCharacter(mother.CharacterObject);
            if (mixFather == null)
                mixFather = Create(father.CharacterObject);
            if (mixMother == null)
                mixMother = Create(mother.CharacterObject);

            Dictionary<int, double> raceMix = new Dictionary<int, double>();
            foreach (KeyValuePair<int, double> race in mixFather.Races)
            {
                double motherValue = mixMother.Races.ContainsKey(race.Key) ? mixMother.Races[race.Key] : 0.0;
                raceMix[race.Key] = (race.Value + motherValue) / 2.0;
            }
            foreach (KeyValuePair<int, double> race in mixMother.Races)
            {
                if (!raceMix.ContainsKey(race.Key))
                    raceMix[race.Key] = race.Value / 2.0;
            }
            Create(hero.CharacterObject, raceMix);
        }

        // Creates a new CharacterRacialMix for the given character.
        private static CharacterRacialMix Create(CharacterObject character, Dictionary<int, double> raceMix = null)
        {
            if (raceMix == null)
            {
                raceMix = new Dictionary<int, double>()
                {
                    { ((BasicCharacterObject)character).Race, 100.0 }
                };
            }
            CharacterRacialMix newMix = new CharacterRacialMix(character)
            {
                Races = raceMix
            };
            RacialMixingBehavior.Instance.AllCharacterRacialMixes[character] = newMix;
            newMix.SetCharacterRace();
            return newMix;
        }
    }
}