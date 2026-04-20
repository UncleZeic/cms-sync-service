# Kubernetes Manifests

These manifests show a production-scaling deployment shape for the CMS Sync Service:

- API deployment with two replicas
- ClusterIP service for internal API traffic
- Ingress for external L7 routing into the API service
- HorizontalPodAutoscaler for API scaling
- PostgreSQL StatefulSet and service for demo persistence
- Redis deployment and service for distributed cache
- RabbitMQ deployment and service for asynchronous CMS event processing
- ConfigMap for non-sensitive settings
- Example Secret for connection strings and auth users

Before applying, copy `secret.example.yaml` to a private secret manifest or create the secret through your deployment pipeline. Do not commit real secrets.

Update `api-deployment.yaml` with the image published by your CI/CD pipeline:

```yaml
image: ghcr.io/your-org/cms-sync-service:latest
```

Apply the manifests:

```sh
kubectl apply -k k8s
```

The API is stateless, so traffic does not require sticky sessions. Redis carries distributed cache state and RabbitMQ carries asynchronous event work.

For real production, prefer managed PostgreSQL, Redis, and RabbitMQ where available. Run EF Core migrations from CI/CD or a dedicated Kubernetes Job before rolling out a new API version.
