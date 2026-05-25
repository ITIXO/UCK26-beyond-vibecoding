# syntax=docker/dockerfile:1.7

FROM node:24-slim AS build
WORKDIR /app

ARG VITE_API_URL
ARG NPM_TOKEN
ENV VITE_API_URL=$VITE_API_URL

COPY package.json package-lock.json ./

# Create .npmrc with registry and auth token for private packages
RUN echo "registry=https://registry.npmjs.org/" >> .npmrc
RUN echo "//pkgs.dev.azure.com/itixo/_packaging/internal/npm/registry/:_authToken=${NPM_TOKEN}" >> .npmrc

RUN npm i --legacy-peer-deps

RUN rm -f .npmrc

COPY . ./
RUN npm run build

RUN npm prune --production

FROM nginx:1.31-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
