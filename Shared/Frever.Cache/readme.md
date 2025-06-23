# Caching architecture

This documents describes basic caching architecture for Frever Client service. This architecture can be not applicable
for other services.

For example for video service there are special cache (paged) which possibly would be added to client service later. For
main service there is separated cache (which would be deleted after splitting). For admin service there should be no
cache but cache resetting only.

## Basic concepts

- **Cache strategy** - defines how data are stored: in memory or in redis, is it possible to get only part of data etc.
- **Dependency** - it's a type, usually entity (represented db table) which is used to build up cached data.
- **Cache resetting** - removing data from cache to refresh content with updated data. Basic rule - everything you have
  to do to reset a cache - delete a key. That allows to simplify resetting and also allows to track dependencies.
  Usually keys are deleted by prefix due resetting.

## Strategies

### Blob

With blob data stored in cache as a single big object. To access an object internals you need to get object from cache
first.

There are two storage for blob - memory and Redis. With memory storage the data stored in RAM and only flag is stored in
Redis to allow cache reset and expiration.

## Cache resettings

The most complex part. Basically there are few options of resetting:

- Reset by key. Internally that's resetting by key prefix to allow resetting cache for all users (user key is build as a
  basic key + user_id as suffix)
- Reset by dependency. Internally dependency is stored in redis as a reverse dictionary:
  having dependency name as a key and all related cache keys as a list of values. On resetting dependency we get all
  keys from dependency name and delete them from cache.

**Important rules for cache resetting**

- Cache resetting should be done by key name only. It shouldn't depend on cache type or internal structure. Otherwise it
  would be extremely complex.
- Cache resetting should be available without configuration. In other words cache resetting should work from foreign
  assembly where per-type cache is not configured.