# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /app

ARG VITE_API_URL=http://localhost:5080
ENV VITE_API_URL=$VITE_API_URL

COPY package.json package-lock.json .npmrc ./
RUN --mount=type=secret,id=npmrc,target=/root/.npmrc npm ci

COPY . ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
