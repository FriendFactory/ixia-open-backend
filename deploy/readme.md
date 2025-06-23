# Deploymnent

The files in this folder creates environment and required services and deploys frever application there.

## Prerequisites

- `kubectl` - Kubernetes CLI
- `docker` (and `docker-compose`) to build and push an application
- `terraform` to create infrastructure
- `aws cli` (`eks`, `ecs`)
- `aws-iam-authenticator`
- `helm`

## Environment management

### Creating environment

To run Frever on empty AWS account you'll need to do following steps:

- **Create frever environment** - this includes creating kubernetes cluster, CDN, RDS, VPC etc.
- **Create environment extras** - deploys some utils services to kubernetes cluster. Those extras are each single per cluster.
- (Optional) **Copy DB** - if you plan to use internal db server, you'll need to copy data from one of public servers
- **Build Frever** - build and push docker images for Frever services
- **Deploy Frever** - deploys helm chart with Frever application to kubernetes
- **Deploy extras to environment** - deploys supplementary diagnostics tools like Prometheus and Jaeger

### Environment destruction

- Uninstal application by running `helm uninstall <release-name>`
- Wait for ~10 min until DNS recors and ALB would be destroyed
- Go to AWS console and ensure all ALB got destroyed. Otherwise it will cause destruction to fail
- Run `./deploy-app.sh` script
- Pray

#### Manual destruction

Sometimes terraform fails and no longer able to maintain a configuration.
In such case it could be impossible to delete an environment via calling `terraform destroy` and environment must be deleted manually.

- Uninstall all installed Helm releases:
  - `helm list --all-namespaces` to see all installed releases
  - `helm uninstall <release-name> -n <release-namespace>` to remove
- Wait ~10 min to destroy related resources (Application Load Balancers and Route53 records)
  - Go to AWS Console and ensure ALB are deleted. Otherwise it prevents from VPC deleteion
- Delete Kubernetes cluster
  - This will require prior deleting Node Groups and Fargate Profiles on cluster
- Delete CloudFront distribution
  - You need to disable it first
- Delete DB Server instance if any
- Delete RDS Subnet group (see RDS left menu -> Subnet groups) (otherwise you'd have a lot of fun finding what's it)
- Delete ElastiCache Redis cluster
- Delete ElastiCache Subnet group
- Delete VPC and related objects
  - Probably will need to delete NAT Gateway
- Delete CloudWatch log groups
- Delete Lambda functions
- Delete IAM Resources
  - Filter policies by cluster name (fe _dev-k8s_) and delete them all
  - Delete `KubernetesExternalDNS` IAM Policy
  - Delete Roles with Cluster name in name
  - Delete Identity Providers. A bit tricky because there is no identification and need to check provides of existing cluster.
- Delete ECR Repositories
- Delete Route 53 DNS Record for `xxxxxxxxx.com` zone
- Delete SQS queues
- Delete Media Convert queue
- Delete triggers from bucket
- Delete rule in Event Bridge

### Useful links

<https://blog.freshtracks.io/a-deep-dive-into-kubernetes-metrics-b190cc97f0f6>

## Application

### CI/CD

Jenkins link https://ci.frever-api.com

There is a tasks for updating each environment
On build you should specify either commit or remote name to build (if specifying branch start it with `origin/`)

### Manual publish

There is script `deploy/scripts/app/publish.sh` or publishing.
It deploys current checked out version to specified environment.

Example usage

```
./publish.sh content-stage
```

### Rolling back to previous version

Deploy is performed via `helm`. Rollback is performed via the helm.

Script: `deploy/scripts/app/rollback.sh`

Example - list all version on dev-1:

`./rollback.sh dev-1`

Example - rollback to certain revision

`./rollback.sh dev-1 88`

## Common cases

Text below describes some common service cases and how to do that

### CASE: Deploy new version from branch or commit

1. Go to Jenkins [https://ci.frever-api.com](https://ci.frever-api.com)

2. Choose environment

3. Press `Build with parameters` and enter either:

   3.1 Branch name prefixed with `origin/` to build latest commit in branch
   3.2 Commit hash to build certain revision

4. Wait until build end

5. If there are any errors due building,
   go to `Console Output` and try to find error reason

6. Wait ~5-10 min until k8s restarts application

7. Open file `Microservices/Admin/Frever.AdminService.Api/Http/test-env-after-deploy.http` with Visual Studio Code (install REST Client extensions)

8. Change `@auth-server` (second line) to name of environment you'd deployed

9. Run basic tests from the file (login, all healthchecks, one-two GET requests for each microservice)

### CASE: Rollback to previous version

1. Go to Jenkins [https://ci.frever-api.com](https://ci.frever-api.com)

2. Choose environment

3. Find a version to rollback to

    3.1 If you know commit hash you could simply deploy env from that commit

    3.2 Otherwise find a build you'd like to re-deploy

    3.3. Open build details, on build details page you'll see two commit hashes

    3.4 Copy second one

4. Deploy environment from that commit

### CASE: Figure out why deployed application doesn't work

1. Go to `deploy/environment/frever` folder

2. All commands below must be prefixed with following: `env KUBECONFIG=kubeconfig_<env-name>`,
   for example for dev-1 it should be `env KUBECONFIG=kubeconfig_dev-1`

   Example command `env KUBECONFIG=kubeconfig_dev-1 kubectl get all -n dev-1`

   Below I will omit that prefix but it mandatory

3. Get all resources

   `kubectl get all -n dev-1`

   Take a look on pods/xxx section (the first one)
   All pods should be with status Running and Restarts should be zero or some
   small number (below 10)

4. If some pod is in some other status (except ContainerCreating or Terminating, which means pod is in process or starting/stopping) you could see the logs:

5. See the pod logs:

  `kubectl logs pods/<pod-name> -n <env-name>`

  Example

  `kubectl logs pod/asset-deployment-68df6c844b-g48d4 - dev-1`

  The .net core logs would be shown

### CASE: Add new config value

There are two phases of adding new config value:

**Declaration of new config value**

New config value should be added in following places:

1. To `deploy/application/helm-chart/frever-app/values.yaml` file (that's Helm requirement)

2. To config map `deploy/application/helm-chart/frever-app/templates/configmap.yaml` where you could reference value from values.yaml

3. To corresponding service (`<service-name>.yaml`) where you could reference config map value (see many examples inside all three files)

**Be very careful with indentation, keep exactly the same indentation as similar values, otherwise it could cause hard to understand errors**

**Setting up value for environment**

1. If new config value should be the same for all environment, you could set the value directly in values.yaml

2. If config value should be different per environment, use correspoding YAML file from `deploy/configs/clusters`. Again be very careful with indentation and use the same names as in values.yaml

