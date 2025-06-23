{{- define "frever-app.service-host" -}}
{{ $.Values.clusterName }}.{{ $.Values.serverDomain }}
{{- end }}

{{- define "frever-app.service-external-url" -}}
https://{{ $.Values.clusterName }}.{{ $.Values.serverDomain }}/{{ .Values.apiIdentifier }}/{{ .Service }}/
{{- end }}

{{- define "frever-app.connection-string" -}}
Host={{ .server }};Username={{ .user }};Password={{ .password }};Database={{ .db }};Maximum Pool Size=30;
{{- end }}

{{- define "frever-app.resources" -}}
          {{ if .Values.serverInfo.limitResources  -}}
          {{- with .Values.serverInfo }}
          resources:
            limits:
              cpu: "{{ floor (divf (mulf .minNodeCount .cpuPerNode .cpuThreshold .compactRatio) (mulf .serviceCount .minReplicas)) }}m"         # (minNodeCount * cpuPerNode * cpuThreshold * compactRatio) / (serviceCount * replicas)
              memory: "{{ floor (divf (mulf .minNodeCount .memoryPerNode .memoryThreshold .compactRatio) (mulf .serviceCount .minReplicas)) }}Mi"         # (minNodeCount * cpuPerNode * cpuThreshold * compactRatio) / (serviceCount * replicas)
            requests:
              cpu: "{{ floor (divf (divf (mulf .minNodeCount .cpuPerNode .cpuThreshold .compactRatio) (mulf .serviceCount .minReplicas)) 6) }}m"         # limit / 6
              memory: "{{ floor (divf (divf (mulf .minNodeCount .memoryPerNode .memoryThreshold .compactRatio) (mulf .serviceCount .minReplicas)) 6) }}Mi"         # limit / 6
          {{- end }}
          {{- end }}
{{- end}}
