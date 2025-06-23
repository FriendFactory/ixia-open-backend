# Video and Asset geo splitting

## Problem 1: We need to only show Videos User can understand

- To do that we need to know languages User understands
    - We could infer those from User Country initially
    - Probably later we could infer that from User activity via ML
    - Possible to add Languages in application settings
- We need to know the language of the video.
  Probably can infer via default language by author User country or via ML or via special API.

Questions and corner cases:

- Should we mark Video as language-specific if it doesn't have captions, user sounds or descriptions?

## Problem 2: We need to restrict audience of certain videos.

For example, we might want to show Swedish videos (even in English) only in Sweden.
Or we might want to don't show videos related to some local events in other countries.
Or we might want to don't show low quality videos to global audience until it gets certain amount of reactions locally.

Or vice versa we might want to show videos in English to (all?) countries.

## Problem 3: We need restricted access to Assets (Templates, Songs, Set Locations etc.)

Videos created using restricted Assets should inherit those restrictions.

# Questions

- Will country and language be passed from device on registering account?
- Would it be possible to change country or language later?
- Do we need to add extra language for user now?
