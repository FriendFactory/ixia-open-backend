using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Frever.Client.Shared.AI.ComfyUi.Contract;

namespace Frever.Client.Shared.AI.ComfyUi;

public static class Workflows
{
    private static readonly ReadOnlyDictionary<string, WorkflowDefinition> Definitions = InitializeDefinitions();

    public static class Keys
    {
        public const string Flux = "flux-prompt";
        public const string FluxPhoto = "flux-photo-prompt";
        public const string FluxPhotoRedux = "flux-photo-redux-style";
        public const string PulidTwoChars = "photo-pulid-2";
        public const string PulidThreeChars = "photo-pulid-3";
        public const string MakeUp = "photo-make-up-thumbnails";
        public const string MakeUpSkin = "photo-make-up-skin-thumbnails";
        public const string MakeUpLips = "photo-make-up-lips-thumbnails";
        public const string MakeUpEyebrows = "photo-make-up-eyebrows-thumbnails";
        public const string MakeUpEyelashes = "photo-make-up-eyelashes-eyeshadow-thumbnails";
        public const string AcePlus = "photo-ace-plus";
        public const string SonicText = "sonic-text";
        public const string SonicAudio = "sonic-audio";
        public const string StillImageText = "still-image-text";
        public const string StillImageAudio = "still-image-audio";

        public const string MusicGen = "music-gen";
        public const string MmAudio = "mm-audio";
        public const string LipSync = "latent-sync";
        public const string LipSyncText = "latent-sync-text";
        public const string LivePortrait = "video-live-portrait";
        public const string AudioToVideo = "video-on-output-audio";
        public const string TextToVideo = "video-on-output-text";
    }

    public static IEnumerable<WorkflowDefinition> All => Definitions.Values;

    public static IEnumerable<string> AllKeys => Definitions.Keys;

    public static IEnumerable<string> PhotoWorkflows => Definitions.Values.Where(w => w.Type == ComfyUiType.Photo).Select(w => w.Key);

    public static IEnumerable<string> VideoWorkflows => Definitions.Values.Where(w => w.Type == ComfyUiType.Video).Select(w => w.Key);

    public static IEnumerable<string> ImageOutputWorkflows => Definitions.Values.Where(w => w.HasImageOutput).Select(w => w.Key);

    public static IEnumerable<string> VideoOutputWorkflows => Definitions.Values.Where(w => w.HasVideoOutput).Select(w => w.Key);

    public static IEnumerable<string> WithNarrationWorkflows => Definitions.Values.Where(w => w.WithNarration).Select(w => w.Key);

    public static IEnumerable<string> WithAudioWorkflows => Definitions.Values.Where(w => w.WithSound).Select(w => w.Key);

    public static bool TryGetSubject(string key, out string subject)
    {
        subject = Definitions.TryGetValue(key, out var definition) ? definition.Subject : null;
        return subject != null;
    }

    public static bool TryGetMessageType(string key, out Type messageType)
    {
        if (Definitions.TryGetValue(key, out var definition))
        {
            messageType = definition.MessageType;
            return messageType != null;
        }

        messageType = null;
        return false;
    }

    private static ReadOnlyDictionary<string, WorkflowDefinition> InitializeDefinitions()
    {
        var definitions = new Dictionary<string, WorkflowDefinition>();

        Add(
            Keys.Flux,
            ComfyUiType.Photo,
            "ComfyUiFluxPromptMessage",
            typeof(ComfyUiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.FluxPhoto,
            ComfyUiType.Photo,
            "ComfyUiFluxPhotoPromptMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.FluxPhotoRedux,
            ComfyUiType.Photo,
            "ComfyUiFluxPhotoReduxStyleMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.PulidTwoChars,
            ComfyUiType.Photo,
            "ComfyUiPhotoPulidMultiCharMessage",
            null,
            true
        );
        Add(
            Keys.PulidThreeChars,
            ComfyUiType.Photo,
            "ComfyUiPhotoPulidMultiCharMessage",
            null,
            true
        );
        Add(
            Keys.MakeUp,
            ComfyUiType.Photo,
            "ComfyUiPhotoMakeUpThumbnailsMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.MakeUpSkin,
            ComfyUiType.Photo,
            "ComfyUiPhotoMakeUpSkinThumbnailsMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.MakeUpLips,
            ComfyUiType.Photo,
            "ComfyUiPhotoMakeUpLipsThumbnailsMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.MakeUpEyebrows,
            ComfyUiType.Photo,
            "ComfyUiPhotoMakeUpEyebrowsThumbnailsMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.MakeUpEyelashes,
            ComfyUiType.Photo,
            "ComfyUiPhotoMakeUpEyelashesEyeshadowThumbnailsMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );
        Add(
            Keys.AcePlus,
            ComfyUiType.Photo,
            "ComfyUiPhotoAcePlusMessage",
            typeof(ComfyUiMultiPhotoAndPromptMessage),
            true
        );

        Add(
            Keys.SonicText,
            ComfyUiType.Photo,
            "ComfyUiSonicTextMessage",
            typeof(ComfyUiInputAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasNarration: true
        );
        Add(
            Keys.SonicAudio,
            ComfyUiType.Photo,
            "ComfyUiSonicAudioMessage",
            typeof(ComfyUiInputAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.StillImageText,
            ComfyUiType.Photo,
            "ComfyUiStillImageTextMessage",
            typeof(ComfyUiInputAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasNarration: true
        );
        Add(
            Keys.StillImageAudio,
            ComfyUiType.Photo,
            "ComfyUiStillImageAudioMessage",
            typeof(ComfyUiInputAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasAudio: true
        );

        Add(
            Keys.MusicGen,
            ComfyUiType.Video,
            "ComfyUiMusicGenMessage",
            typeof(ComfyUiMusicGenMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.MmAudio,
            ComfyUiType.Video,
            "ComfyUiMmAudioMessage",
            typeof(ComfyUiInputAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.LipSync,
            ComfyUiType.Video,
            "ComfyUiLatentSyncMessage",
            typeof(ComfyUiLatentSyncMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.LipSyncText,
            ComfyUiType.Video,
            "ComfyUiLatentSyncTextMessage",
            typeof(ComfyUiLatentSyncMessage),
            hasVideoOutput: true,
            hasNarration: true
        );
        Add(
            Keys.LivePortrait,
            ComfyUiType.Video,
            "ComfyUiVideoLivePortraitMessage",
            typeof(ComfyUiVideoLivePortraitMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.AudioToVideo,
            ComfyUiType.Video,
            "ComfyUiVideoOnOutputAudioMessage",
            typeof(ComfyUiVideoAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasAudio: true
        );
        Add(
            Keys.TextToVideo,
            ComfyUiType.Video,
            "ComfyUiVideoOnOutputTextMessage",
            typeof(ComfyUiVideoAndAudioAndPromptMessage),
            hasVideoOutput: true,
            hasNarration: true
        );

        return new ReadOnlyDictionary<string, WorkflowDefinition>(definitions);

        void Add(
            string key,
            ComfyUiType type,
            string subject,
            Type messageType = null,
            bool hasImageOutput = false,
            bool hasVideoOutput = false,
            bool hasNarration = false,
            bool hasAudio = false
        )
        {
            definitions[key] = new WorkflowDefinition(
                key,
                type,
                subject,
                messageType,
                hasImageOutput,
                hasVideoOutput,
                hasNarration,
                hasAudio
            );
        }
    }

    public sealed record WorkflowDefinition(
        string Key,
        ComfyUiType Type,
        string Subject,
        Type MessageType,
        bool HasImageOutput = false,
        bool HasVideoOutput = false,
        bool WithNarration = false,
        bool WithSound = false
    );

    public enum ComfyUiType
    {
        Photo,
        Video
    }
}

public class WorkflowKey
{
    public const string ImageFromPrompt = "image-from-prompt";
    public const string ImageFromPromptAnd1Image = "image-from-prompt-and-1-image";
    public const string ImageFromPromptAnd2Images = "image-from-prompt-and-2-images";
    public const string ImageFromPromptAnd3Images = "image-from-prompt-and-3-images";
    public const string Makeup = "makeup";
    public const string Outfit = "outfit";
    public const string LipSyncVideoFromImageAndAudio = "lip-sync-video-from-image-and-audio";
    public const string LipSyncVideoFromImageAndTextSpeech = "lip-sync-video-from-image-and-text-speech";
    public const string LipSyncVideoFromVideoAndAudio = "lip-sync-video-from-video-and-audio";
    public const string VideoFromVideoAndAudio = "video-from-video-and-audio";
    public const string VideoFromVideoAndNarrationVoice = "video-from-video-and-narration-voice";
    public const string VideoFromVideoAndGeneratedAudio = "video-from-video-and-generated-audio";
    public const string VideoFromVideoAndGeneratedSfx = "video-from-video-and-generated-sfx";
    public const string VideoFromImageAndPrompt = "video-from-image-and-prompt";
    public const string VideoFromImageAndTextSpeech = "video-from-image-and-text-speech";
    public const string VideoFromImageAndAudio = "video-from-image-and-audio";
    public const string CharacterFromPrompt = "character-from-prompt";
    public const string CharacterFromImageAndPrompt = "character-from-image-and-prompt";
    public const string ImageBasedOnStyle = "image-based-on-style";
}