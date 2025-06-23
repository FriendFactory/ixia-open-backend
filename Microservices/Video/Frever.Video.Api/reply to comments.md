# Reply to comment feature

This documents describes API and contains short documentation of new feature

## Adding reply

Adding new reply is similar to adding root level comment

Add reply:

```
POST {{video_server}}video/{{videoId}}/comment
Authorization: Bearer XXX
Content-Type: application/json

{
  "Text": "Reply 217",
  "ReplyToCommentId": 217
}
```

Adding root comment:

```
POST {{video_server}}video/{{videoId}}/comment
Authorization: Bearer XXX
Content-Type: application/json

{
  "Text": "Reply 217",
  "ReplyToCommentId": null
}

```

## Getting list of comment

There are two API: one for getting list of root comments and another for get a part of list around certain comment:

### Root comments

Get root comments from newest to oldest:

```
GET {{video_server}}video/{{videoId}}/comment/root
Authorization: Bearer XXX
```

Response example:

```
[
  {
    "id": 235,
    "videoId": 1766,
    "groupId": 16,
    "time": "2022-04-21T08:40:26.293154+00:00",
    "text": "Mention @373 Viktor's old group 2",
    "groupNickname": "horun_gmail",
    "mentions": [
      {
        "groupId": 373,
        "name": "aengmo99",
        "nickname": "aengmo99"
      }
    ],
    "key": "0000GQ",
    "replyCount": 0,
    "replyToComment": null
  },
  {
    "id": 226,
    "videoId": 1766,
    "groupId": 16,
    "time": "2022-04-20T13:30:50.854837+00:00",
    "text": "Top level comment 5",
    "groupNickname": "horun_gmail",
    "mentions": [],
    "key": "0000GH",
    "replyCount": 4,
    "replyToComment": null
  }
]
```

Also it's possible to get a range of comments before and after certain comment:

```
GET {{video_server}}video/{{videoId}}/comment/root
    ?key=0000GF
    &takeNewer=2
    &takeOlder=2
Authorization: Bearer XXX
```

You should use `key` field in comment from response to pass to that API

## Replies

You could get a list of replies to root comment using following API.
Replies always arranged from oldest to newest and replies to replies and deeper are flatten to list.

Get a list of replies starting from the oldest one:

```
GET {{video_server}}video/{{videoId}}/comment/thread/0000FA
Authorization: Bearer XXX
```

You should pass root comment key after `/thread/<here>`

Also it's possible to get a range of comments aroung certain comment:

```
GET {{video_server}}video/{{videoId}}/comment/thread/0000FA
  ?key=0000FA.0000FE.0000G9
  &takeOlder=2
  &takeNewer=2
Authorization: Bearer XXX
```

In request above the `0000FA` is `key` of the root comment
and `0000FA.0000FE.0000G9` is `key` of one of replies

The response looks like that:

```
[
  {
    "id": 217,
    "videoId": 1766,
    "groupId": 576,
    "time": "2022-04-19T18:17:44.242771+00:00",
    "text": "Hi im level2 reply to comment 187",
    "groupNickname": "🪱🪖",
    "mentions": [],
    "key": "0000FA.0000FE.0000G8",
    "replyCount": 1,
    "replyToComment": {
      "commentId": 187,
      "groupId": 16,
      "groupNickname": "horun_gmail"
    }
  },
  ...
```

Please note that response contains nested object `replyToComment`.
It contains id of comment were replied, and also information about user who wrote the comment were replied.

Also new API doesn't contain total count of comments.
Approximate number of comments could be got from video details but you should not use it for pagination.
Instead use infinity scrolling approach as with video.

## C# classes definitions

Comment user info:

```
public class UserCommentInfo
{
    public long Id { get; set; }

    public long VideoId { get; set; }

    public long GroupId { get; set; }

    public DateTime Time { get; set; }

    public string Text { get; set; }

    public string GroupNickname { get; set; }

    [ProtoNewField(1)] public List<Mention> Mentions { get; set; }

    /// <summary>
    ///     Use this field to paginate comments
    /// </summary>
    [ProtoNewField(2)]
    public string Key { get; set; }

    [ProtoNewField(3)] public int ReplyCount { get; set; }

    [ProtoNewField(4)] public CommentGroupInfo ReplyToComment { get; set; }
}
```

Comment group info:

```
public class CommentGroupInfo
{
    /// <summary>
    ///     Gets or sets ID of comment replied
    /// </summary>
    public long CommentId { get; set; }

    /// <summary>
    ///     Gets or sets group ID of user had been written the comment replied
    /// </summary>
    public long GroupId { get; set; }

    /// <summary>
    ///     Gets or sets group nickname of user had been written the comment replied
    /// </summary>
    public string GroupNickname { get; set; }
}
```