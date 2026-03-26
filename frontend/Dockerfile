FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npx ng build --configuration=production

# Debug: mostra o que foi gerado
RUN ls -la dist/ && ls -la dist/Materialpro/ && ls -la dist/Materialpro/browser/ || true

FROM nginx:alpine

# Copiar build output - tenta browser/ primeiro, senao pega a raiz
COPY --from=build /app/dist/Materialpro/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Verificar se index.html existe
RUN ls -la /usr/share/nginx/html/index.html

EXPOSE 80
CMD sh -c "sed -i \"s/listen 80/listen ${PORT:-80}/\" /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"
