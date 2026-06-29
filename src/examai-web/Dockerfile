# Stage 1: Build Angular 18
FROM node:22-alpine AS build
WORKDIR /app

COPY src/frontend/package*.json ./
RUN npm ci

COPY src/frontend/ .
RUN ./node_modules/.bin/ng build --configuration=production

# Stage 2: Deployment via Nginx Non-Root
FROM nginx:alpine

# עדכון הגדרות Nginx לריצה כמשתמש מוגבל בפורט 8080
RUN sed -i 's/listen\.+80;/listen 8080;/g' /etc/nginx/conf.d/default.conf && \
    chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d

RUN touch /var/run/nginx.pid && chown -R nginx:nginx /var/run/nginx.pid

# העתקת קובץ הגדרות הניתוב של סינגל פייג' אפליקיישן (SPA)
COPY src/frontend/nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/exam-ai/browser /usr/share/nginx/html

USER nginx
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/ || exit 1