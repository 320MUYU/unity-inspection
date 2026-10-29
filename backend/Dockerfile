FROM golang:1.23 AS build
WORKDIR /src
COPY go.mod .
COPY *.go ./
RUN CGO_ENABLED=0 go build -o /server .
FROM alpine:3.21
COPY --from=build /server /server
ENV DATA_FILE=/data/issues.json
EXPOSE 8080
ENTRYPOINT ["/server"]
