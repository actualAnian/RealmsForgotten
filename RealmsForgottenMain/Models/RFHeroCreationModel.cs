using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    /// <summary>
    /// Conserta o filho de casamento inter-racial (achado do estudo do LOTRAOM, 2026-08-18,
    /// confirmado no decompilado 1.4.8).
    ///
    /// O vanilla monta o template do recém-nascido a partir do pai do mesmo sexo (menino→pai,
    /// menina→mãe), então a RAÇA do filho é a desse pai. Mas
    /// DefaultHeroCreationModel.GetStaticBodyProperties gera a APARÊNCIA com
    /// hero.Mother.CharacterObject.Race incondicionalmente, misturando ainda os BodyProperties
    /// da mãe e do pai como min/max — entre raças diferentes, são espaços faciais
    /// incompatíveis. Resultado: um menino de mãe humana × pai orc nasce com Race orc e rosto
    /// gerado no espaço facial humano — corpo/rosto corrompido. Com as raças do RF_Races e o
    /// casamento vanilla (que não filtra raça), isso acontece sozinho na campanha.
    ///
    /// A correção age SÓ no caso de pais de raças diferentes: gera a aparência na raça que o
    /// filho realmente tem (a do template) e usa como semente apenas o pai da MESMA raça, em
    /// vez de misturar espaços incompatíveis. Pais da mesma raça seguem o caminho vanilla
    /// intocado.
    /// </summary>
    public class RFHeroCreationModel : DefaultHeroCreationModel
    {
        public override StaticBodyProperties GetStaticBodyProperties(Hero hero, bool isOffspring, float variationAmount = 0.35f)
        {
            if (isOffspring && hero?.Mother != null && hero.Father != null
                && hero.Mother.CharacterObject != null && hero.Father.CharacterObject != null
                && hero.Mother.CharacterObject.Race != hero.Father.CharacterObject.Race)
            {
                return GetMixedRaceOffspringBodyProperties(hero, variationAmount);
            }

            return base.GetStaticBodyProperties(hero, isOffspring, variationAmount);
        }

        private static StaticBodyProperties GetMixedRaceOffspringBodyProperties(Hero hero, float variationAmount)
        {
            // A raça do filho já foi decidida pelo template (pai do mesmo sexo). Toda a
            // geração de aparência acontece NESSA raça.
            //
            // ⚠️ CTD 2026-08-19 (assert HeroCreator.cs:272 + morte no facegen nativo):
            // a 1ª versão semeava com os BodyProperties DINÂMICOS do pai da mesma raça
            // (min=max=pai) — combinação que o facegen nativo nunca roda no vanilla e
            // que derrubou o jogo num parto inter-racial de campanha. Agora o rosto sai
            // do range do PRÓPRIO template (min/max do XML), o caminho exato que o jogo
            // usa para criar qualquer herói de template — nativo comprovado para todas
            // as raças do RF. Perde-se a semelhança com o pai; ganha-se não crashar.
            CharacterObject template = hero.CharacterObject.OriginalCharacter ?? hero.CharacterObject;
            int race = template.Race;

            Debug.Print($"[RF] Offspring inter-racial: mae={hero.Mother?.CharacterObject?.Race}, pai={hero.Father?.CharacterObject?.Race}, filho race={race}, template={template.StringId}");

            string hairTags = template.BodyPropertyRange?.HairTags ?? string.Empty;
            string beardTags = template.BodyPropertyRange?.BeardTags ?? string.Empty;
            string tattooTags = template.BodyPropertyRange?.TattooTags ?? string.Empty;

            BodyProperties generated = BodyProperties.GetRandomBodyProperties(
                race, hero.IsFemale,
                template.GetBodyPropertiesMin(returnBaseValue: true),
                template.GetBodyPropertiesMax(returnBaseValue: true),
                1, MBRandom.RandomInt(), hairTags, beardTags, tattooTags, variationAmount);

            // Cabelo/barba/tatuagem da cultura, como o vanilla faz quando as tags estão vazias.
            int hair = -1;
            int beard = -1;
            int tattoo = -1;
            if (string.IsNullOrEmpty(hairTags))
            {
                int[] options = Campaign.Current.Models.BodyPropertiesModel.GetHairIndicesForCulture(race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
                hair = options.Length != 0 ? options[MBRandom.RandomInt(options.Length)] : -1;
            }
            if (string.IsNullOrEmpty(beardTags))
            {
                int[] options = Campaign.Current.Models.BodyPropertiesModel.GetBeardIndicesForCulture(race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
                beard = options.Length != 0 ? options[MBRandom.RandomInt(options.Length)] : -1;
            }
            if (string.IsNullOrEmpty(tattooTags))
            {
                int[] options = Campaign.Current.Models.BodyPropertiesModel.GetTattooIndicesForCulture(race, hero.IsFemale ? 1 : 0, hero.Age, hero.Culture);
                tattoo = options.Length != 0 ? options[MBRandom.RandomInt(options.Length)] : -1;
                float zeroChance = FaceGen.GetTattooZeroProbability(race, hero.IsFemale ? 1 : 0, hero.Age);
                if (MBRandom.RandomFloat < zeroChance)
                {
                    tattoo = 0;
                }
            }

            FaceGen.SetHair(ref generated, hair, beard, tattoo);
            return generated.StaticProperties;
        }
    }
}
