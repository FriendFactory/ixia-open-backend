# New commenting features

## Comment replies

### Data model changes:

- Investigate usage of `ltree` column to store comment hierarchy

### API model changes

- Add `SortingId` column to comment to support paging with custom order
- Add `Replies` field to comments to provide first page of replies with comment
- Add new API to load page of replies by root comment
  (since we have only one visual level of nesting,
  we need API that flattens all comments to a single thread)

## Comment likes

### Data model changes:

- Add new table `CommentLike`similar to video like
- Add field `TotalLikes` to `Comments` table
  increments/decrements each time comment liked/disliked

### API model changes

- Add `TotalLikes` to `Comment` entity
- Add `IsLikedByCurrentUser` boolean field to `Comment` indicates if current comment were liked by current user

## Comment mentions

### Some pre-requirements:

- Comment mentions must be placed in the specified place in the text
- We may want to show group nickname or group name in place of mention
- The mention must reflect changes in the mentioned group nickname or name

### Implementation:

Comment text should contains mentions in the form `@<groupId>`. For example `@16` or `@5524`. On the client it must be
replaced with actual group name/nickname.

### Data model changes:

- Add new column `CommentMentions` in `Comment` table with array of group Id mentioned in text

### API Model changes

- Add new field with collection of group info mentioned in comment (groupId, name, nickname)

## Pinned comments

### Data model changes

- Add `PinnedComments` column in `Video` table with array of ids of pinned comments
  (Could be a single comment if we 100% sure we wouldn't need more then one pinned comment)

### API changes

- Request for first comments page must include pinned comments at start
- API must be able to fast scroll to pinned comment
  (ie API must use the same addressing scheme as video (targetId + prev + next))