# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- Runtime stage (Angular SSR / Express) ----
FROM node:20-alpine AS final
WORKDIR /app
ENV NODE_ENV=production

# Install only runtime dependencies (Angular SSR + express + proxy live in "dependencies").
COPY package.json package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY --from=build /app/dist ./dist

# The SSR server listens on process.env.PORT (Render injects it); 4000 is the local default.
EXPOSE 4000
CMD ["node", "dist/ideal-weight-nutrition-web/server/server.mjs"]
