# Etapa 1: Construcción
FROM node:18-alpine AS build
WORKDIR /app

# Copia e instala dependencias
COPY package.json package-lock.json ./
RUN npm install

# Copia código fuente completo y compila
COPY . .
RUN npm run build

# Etapa 2: Servidor Web
FROM nginx:alpine
# Copiamos la configuración personalizada del enrutador de Angular
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copiamos el build generado a nginx
# (Nota: El folder dist/ puede varias dependiendo del nombre del proyecto en angular.json.
COPY --from=build /app/dist/maquilease-app/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
