# metrics-server e pre-requisito tecnico do HPA (k8s/hpa.yaml) - sem ele o HPA
# nao consegue ler %CPU/memoria dos pods. Nao vem instalado por padrao no kind.
resource "helm_release" "metrics_server" {
  name       = "metrics-server"
  repository = "https://kubernetes-sigs.github.io/metrics-server/"
  chart      = "metrics-server"
  namespace  = "kube-system"

  set {
    name  = "args[0]"
    value = "--kubelet-insecure-tls"
  }

  set {
    name  = "args[1]"
    value = "--kubelet-preferred-address-types=InternalIP\\,ExternalIP\\,Hostname"
  }

  depends_on = [kubernetes_namespace.oficina]
}
