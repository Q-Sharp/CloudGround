FROM node:26-trixie-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

FROM node:26-trixie-slim
ENV NODE_ENV=production
ENV PORT=8080
WORKDIR /app
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist/cloudground-app-web ./dist/cloudground-app-web
COPY --from=build /app/src/instrumentation.mjs ./src/instrumentation.mjs
USER node
EXPOSE 8080
CMD ["node", "--import", "./src/instrumentation.mjs", "dist/cloudground-app-web/server/server.mjs"]
