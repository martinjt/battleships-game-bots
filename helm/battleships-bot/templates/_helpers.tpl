{{/*
Expand the name of the chart.
*/}}
{{- define "battleships-bot.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "battleships-bot.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "battleships-bot.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "battleships-bot.labels" -}}
helm.sh/chart: {{ include "battleships-bot.chart" . }}
{{ include "battleships-bot.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "battleships-bot.selectorLabels" -}}
app.kubernetes.io/name: {{ include "battleships-bot.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app: battleships-{{ .Values.botName }}
bot: {{ .Values.botName }}
{{- end }}

{{/*
PVC name
*/}}
{{- define "battleships-bot.pvcName" -}}
{{ .Values.botName }}-data
{{- end }}

{{/*
Secret name
*/}}
{{- define "battleships-bot.secretName" -}}
{{ .Values.botName }}-config
{{- end }}
