# Kubernetes Manifests

These manifests show a production-scaling deployment shape for the CMS Sync Service:

- API deployment with two replicas
- ClusterIP service for internal API traffic
- HorizontalPodAutoscaler for API scaling
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
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.example.yaml
kubectl apply -f k8s/redis-deployment.yaml
kubectl apply -f k8s/redis-service.yaml
kubectl apply -f k8s/rabbitmq-deployment.yaml
kubectl apply -f k8s/rabbitmq-service.yaml
kubectl apply -f k8s/api-deployment.yaml
kubectl apply -f k8s/api-service.yaml
kubectl apply -f k8s/api-hpa.yaml
```

For real production, prefer a managed PostgreSQL instance and managed Redis where available. Run EF Core migrations from CI/CD or a dedicated Kubernetes Job before rolling out a new API version.
