# Environment configuration update log
## To fix ALB controller

- Enable full access on security groups.
    - TBD: Determine more precise security rules
    - Concl 1: Helped to add node additional access for ing/eg.
        - TBD: Clarify ing or eg, and exact port
    - Added ElasticLoadBalancingFullAccess policy to Node and Cluster ARN
- Private and Public access -> Public access.
    - TBD: Test if it matters
    - Conclustion: seems doesn't matter
- IPv6: test dev-2 with IPv6 enabled
