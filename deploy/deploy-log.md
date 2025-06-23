# Deploy Log

List of changes to be done on deploying to prod (or other environments)

## Start of history

- Single docker image repository (appservice). Ensure registry is created in ECR
- Single load balancer per environment

  - Update aws load balancer manager in k8s
  - Delete old load balancers
  - Delete old Route 53 records
  - Apply new aws load balancer manager

    - Delete old ingress controller `k delete deployment.apps/aws-alb-ingress-controller -n kube-system`
    - Run `deploy/scripts/app/extra-deploy.sh`.
      Add `exit 1` to script at line 52 to avoid re-installing other extras.

  - Delete Jaeger ALB

- Separated queue for video conversion for environment (optional)
  - Create media converter queue (and update config)
  - Create event bridge binding in AWS
  - Create SQS queue (and update config)
- Update media converter job creator lambda
- Split databases
