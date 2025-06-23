{{- define "jaeger.service-host" -}}
jaeger-{{ .Release.Name }}.{{ $.Values.domain }}
{{- end }}