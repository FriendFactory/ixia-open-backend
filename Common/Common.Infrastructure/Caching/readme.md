# Redis

AWS Redis is not accessible outside VPC (
except [AWS VPN](https://docs.aws.amazon.com/AmazonElastiCache/latest/red-ug/accessing-elasticache.html#access-from-outside-aws))
To run Redis locally, use Docker:

```
docker run --name redis-local -d -p 6379:6379 redis
```