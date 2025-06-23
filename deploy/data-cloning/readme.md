# Data cloning across environments

Allows to copy data (DB rows and S3 files) across environments to allow easy using of different environments and have
a similar data across all environments.

## Complexity

## There are several approaches to cloning:

1. Full backup/restore + asset copying + anonymization

   - pros: Simpler than other approaches
   - cons: Erases changes on target environment

2. Clone with identity mapping. Copy data maintaining existing Id differences between environment

   - pros: Related stuff (templates, levels etc.) remains valid
   - cons: Very complex to implement (highest complexity across all approaches)
   - cons: Might be hard to handle some corner cases (like self-referencing)

3. Clone with identity alignment.
   Copies data and tries to put the same entities under the same row Id,
   possibly overwriting existing data.

   - pros: Simpler than identity mapping
   - cons: Existing stuff for entity being overwritten might be broken.
     Note: probably first time cloning will break a lot of stuff, but next cloning will not break new things.
   - cons: Might be hard to automate for some unique constraints (composite or numeric etc)

## Environment cloning

Environment cloning is a chosen approach to clone.
