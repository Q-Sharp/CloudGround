import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { UndiciInstrumentation } from '@opentelemetry/instrumentation-undici';
import { NodeSDK } from '@opentelemetry/sdk-node';

if (process.env.OTEL_EXPORTER_OTLP_ENDPOINT) {
  new NodeSDK({
    traceExporter: new OTLPTraceExporter(),
    instrumentations: [new HttpInstrumentation(), new UndiciInstrumentation()],
  }).start();
}
