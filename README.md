# Ixia

A microservices backend system for the Ixia project.

## Table of Contents

* [Technologies and Integrations](#technologies-and-integrations)
* [Local Development Setup](#local-development-setup)
* [Cloud Deployment](#cloud-deployment)
* [License](#license)
* [Contact](#contact)
* [Contributing](#contributing)

## Technologies and Integrations

### Core Technologies:
* **.NET Core 3.1** and **.NET 8.0** SDKs
* **C# 12.0, 13.0, 14.0**
* **ASP.NET**
* **PostgreSQL** - Primary database
* **Redis** - Caching and session storage
* **AWS Services** - S3, CloudFront, RDS, ElastiCache, ECS/EKS, IAM, Parameter Store, etc.

### Deployment & Observability
* **Docker** - Containerization
* **Kubernetes** - Container orchestration
* **Terraform** - Infrastructure as Code
* **Helm** - Kubernetes package manager
* **Jenkins** - CI/CD pipeline
* **OpenTelemetry** - Tracing and metrics
* **Jaeger** - Distributed tracing
* **Prometheus** - Monitoring

### Platform Integration
Integrates with our Platform service for recommendations and background processing.
[Repository: Platform](https://github.com/FriendFactory/open-platform)

### Third-Party Services:
* **Google Cloud Project** - Google Sign-In/Sign-Up integration
* **Apple Developer Account** - Apple Sign-In/Sign-Up integration
* **Twilio** - SMS/messaging services
* **Content moderation services** - Text/image moderation
* **PixVerse** (Stable Diffusion) - AI video generation

**Note:** Some third-party services and technologies are optional and not required for core platform functionality. However, their credentials may still be referenced during startup. To avoid initialization errors, provide placeholder or dummy values for these optional service credentials if they're not actively used.

## Local Development Setup

### Prerequisites:

* **.NET Core 3.1** and **.NET 8.0** SDKs
* **Docker Desktop** (recommended) or PostgreSQL + Redis installed locally
* **AWS CLI** configured

### Setup Steps:

1. **Clone and configure:**

```bash
   git clone <repository-url>
   cd ixia-server
   cp .env.example .env
```

1. **Set environment and run:**

```bash
   ./with-env.sh
```

1. **Choose your preferred option:**

   * **Manual:** Install PostgreSQL + Redis locally, then `dotnet run`
   * **Docker Compose:** `docker-compose up -d` (if available)
   * **Local K8s:** Deploy using Helm charts from deploy folder

## Cloud Deployment

### Using Included Deploy Folder (Recommended)
The `deploy/` folder contains ready-to-use deployment configurations including Helm charts, Terraform infrastructure definitions, and Jenkins CI/CD pipelines.
**To use the included deployment:**
1. Update the deployment configuration files with your environment-specific values (database connections, API keys, AWS settings, etc.)
2. Run the deployment scripts from the `deploy/scripts/` folder

All necessary Helm values, Terraform configurations, and Jenkins pipeline settings need to be updated with your specific environment details before deployment.

### Custom Deployment
Alternatively, you can ignore the included deployment folder and implement your own deployment solution. You'll need to set up:
- **AWS Infrastructure:** EKS cluster, RDS PostgreSQL, ElastiCache Redis, S3 buckets, CloudFront distribution, VPC, IAM roles
- **Container orchestration** using Kubernetes or similar
- **CI/CD pipeline** of your choice

For detailed instructions, refer to the documentation in the `deploy/` folder.

## License

This project is licensed under the [LICENSE](./LICENSE).

Please note that the Software may include references to “Frever” and/or “Ixia” and that such terms may be subject to trademark or other intellectual property rights, why it is recommended to remove any such references before distributing the Software.

## Support

This repository is provided as-is, with no active support or maintenance. For inquiries related to the open source project, please contact:

📧 [admin@frever.com](mailto:admin@frever.com)

## Contributing

We welcome forks and reuse! While the platform is no longer maintained by the original team, we hope it serves as a useful resource for:

* Generative media tooling
* AI-driven content flows
* Mobile-first creative platforms

Please open issues or pull requests on individual repos if you want to share fixes or improvements.
