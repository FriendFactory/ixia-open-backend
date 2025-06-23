using System.Threading.Tasks;

namespace AuthServer.Services.UserManaging.NicknameSuggestion;

public interface INicknameSuggestionData
{
    Task<string[]> GetNouns();
    Task<string[]> GetAdjectives();
}

public class HardcodedNicknameSuggestionData : INicknameSuggestionData
{
    private static readonly string[] Nouns =
    [
        "peak", "tropic", "lyric", "vine", "dew", "leaf", "isle", "opus", "snow", "zebra", "music", "coal", "silk", "whiff", "universe",
        "song", "tiara", "mountain", "fire", "sky", "flora", "snap", "quiz", "mosquito", "sun", "tune", "reel", "island", "forest", "fog",
        "ring", "cloud", "tiger", "avatar", "planet", "gem", "knoll", "valley", "yarn", "snake", "smoke", "insect", "post", "hill", "gold",
        "poem", "note", "edit", "meme", "grass", "stream", "lotus", "gif", "jazz", "aries", "bird", "zoom", "whale", "scarf", "tree",
        "vale", "track", "iron", "vlog", "viral", "river", "video", "air", "wind", "myth", "rabbit", "wolf", "warp", "skirt", "harp",
        "jewel", "soil", "animal", "zeal", "xenon", "ant", "worm", "dirt", "frame", "star", "kite", "sand", "photo", "sea", "water",
        "steam", "dog", "pixel", "lake", "fish", "horse", "blog", "light", "fly", "glow", "beach", "scene", "bee", "plant", "bush", "seed",
        "galaxy", "shoot", "desert", "ember", "thunder", "moon", "bear", "reef", "crest", "cat", "year", "moss", "trend", "spider", "veil",
        "cow", "orbit", "oil", "wave", "vibe", "earth", "space", "sheep", "nebula", "anime", "pond", "rock", "drone", "ice", "quark",
        "flower", "clip", "wood", "ocean", "rain"
    ];

    private static readonly string[] Adjectives =
    [
        "vague", "loose", "dull", "pale", "new", "wise", "sly", "shy", "vasty", "snug", "lofty", "teal", "white", "mixed", "giddy",
        "sparky", "spry", "meek", "green", "small", "tall", "chipper", "silky", "tough", "bright", "warm", "odd", "wild", "flat", "daring",
        "dim", "gentle", "perky", "clever", "vast", "sour", "stiff", "rich", "cozy", "brisk", "gross", "rapid", "neat", "ultra", "faint",
        "wiry", "agile", "tiny", "gleam", "yellow", "sage", "tan", "gold", "woven", "buzzy", "zippy", "smart", "noisy", "bold", "fresh",
        "hearty", "clear", "fluffy", "hot", "ivory", "tangy", "quiet", "blunt", "humid", "vivid", "slim", "dark", "coral", "curly", "tame",
        "young", "alive", "spicy", "tight", "vain", "quaint", "tasty", "upbeat", "eager", "pure", "rosey", "stale", "quartz", "icy",
        "quick", "ideal", "smooth", "steep", "bliss", "punchy", "jade", "plum", "violet", "jolly", "peppy", "sharp", "wet", "olive", "chic",
        "fancy", "raw", "light", "hard", "lively", "crisp", "zesty", "active", "wide", "lucid", "khaki", "old", "rare", "navy", "silly",
        "shiny", "thin", "ripe", "merry", "weak", "tart", "lime", "blue", "mild", "fiery", "short", "red", "lush", "damp", "ready", "noble",
        "witty", "happy", "solid", "rough", "stout", "low", "nice", "robust", "keen", "trim", "mauve", "loud", "dynamic", "soft", "dry",
        "wavy", "nifty", "taut", "azure", "juicy", "long", "vibrant", "pink", "jaunty", "plush", "alert", "sweet", "trusty"
    ];

    public Task<string[]> GetNouns()
    {
        return Task.FromResult(Nouns);
    }

    public Task<string[]> GetAdjectives()
    {
        return Task.FromResult(Adjectives);
    }
}