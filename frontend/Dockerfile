# PlayPredict frontend + proxy — imagen productiva (P2.2).
# Multi-stage: Node solo para compilar el SPA (dist/), Caddy en runtime
# sirve los estáticos y proxifica /api/* al backend. Sin Vite/Node en runtime.
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM caddy:2-alpine AS runtime
WORKDIR /srv

COPY --from=build /app/dist /srv/www
COPY Caddyfile /etc/caddy/Caddyfile

EXPOSE 80 443
