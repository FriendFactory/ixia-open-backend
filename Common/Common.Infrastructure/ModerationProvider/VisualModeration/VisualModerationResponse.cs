using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Common.Infrastructure.ModerationProvider.VisualModeration;

internal class VisualModerationResponse
{
    private const float MediaContentSeverityLevel = 0.9f;

    private static readonly string[] ConstantModeratedMediaClasses =
    {
        "general_nsfw",
        "general_suggestive",
        "yes_sexual_activity",
        "yes_realistic_nsfw",
        "yes_female_underwear",
        "yes_bra",
        "yes_panties",
        "yes_negligee",
        "yes_male_underwear",
        "yes_sex_toy",
        "yes_cleavage",
        "yes_female_nudity",
        "yes_male_nudity",
        "yes_female_swimwear",
        "yes_bodysuit",
        "yes_miniskirt",
        "yes_sports_bra",
        "yes_sportswear_bottoms",
        "yes_male_shirtless",
        "yes_sexual_intent",
        "yes_undressed",
        "animal_genitalia_and_human",
        "animal_genitalia_only",
        "animated_animal_genitalia",
        "gun_in_hand",
        "gun_not_in_hand",
        "animated_gun",
        "knife_in_hand",
        "knife_not_in_hand",
        "culinary_knife_not_in_hand",
        "culinary_knife_in_hand",
        "very_bloody",
        "a_little_bloody",
        "other_blood",
        "hanging",
        "noose",
        "human_corpse",
        "animated_corpse",
        "yes_emaciated_body",
        "yes_self_harm",
        "yes_pills",
        "illicit_injectables",
        "medical_injectables",
        "yes_smoking",
        "yes_gambling",
        "yes_drinking_alcohol",
        "yes_alcohol",
        "animated_alcohol",
        "yes_nazi",
        "yes_terrorist",
        "yes_kkk",
        "yes_confederate",
        "yes_middle_finger",
        "yes_child_present"
    };

    [JsonProperty("status")] private List<Status> Status { get; set; }

    public (bool, string) GetModerationResult()
    {
        foreach (var status in Status)
        {
            foreach (var output in status.response.output)
            {
                foreach (var @class in output.classes)
                {
                    if (ConstantModeratedMediaClasses.All(x => x != @class.@class))
                        continue;

                    if (@class.score >= MediaContentSeverityLevel)
                        return (false, @class.@class);
                }
            }
        }

        return (true, string.Empty);
    }
}