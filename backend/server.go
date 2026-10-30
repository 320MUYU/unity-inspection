package main

import (
	"encoding/json"
	"errors"
	"io"
	"log"
	"net/http"
	"os"
	"strings"
)

func jsonResponse(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}
func fail(w http.ResponseWriter, err error) {
	status := http.StatusInternalServerError
	message := "internal server error"
	if errors.Is(err, ErrInvalid) {
		status = 400
		message = err.Error()
	}
	if errors.Is(err, ErrNotFound) {
		status = 404
		message = err.Error()
	}
	if errors.Is(err, ErrConflict) {
		status = http.StatusConflict
		message = err.Error()
	}
	if status == 500 {
		log.Printf("request failed: %v", err)
	}
	jsonResponse(w, status, map[string]string{"error": message})
}
func decode(r *http.Request, v any) error {
	d := json.NewDecoder(io.LimitReader(r.Body, 1<<20))
	d.DisallowUnknownFields()
	if err := d.Decode(v); err != nil {
		return err
	}
	var extra any
	if err := d.Decode(&extra); err != io.EOF {
		return errors.New("unexpected trailing data")
	}
	return nil
}

func NewHandler(store *Store) http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) { jsonResponse(w, 200, map[string]string{"status": "ok"}) })
	mux.HandleFunc("GET /issues", func(w http.ResponseWriter, r *http.Request) { jsonResponse(w, 200, store.List()) })
	mux.HandleFunc("POST /issues", func(w http.ResponseWriter, r *http.Request) {
		var input CreateIssue
		if err := decode(r, &input); err != nil {
			jsonResponse(w, 400, map[string]string{"error": "invalid JSON request"})
			return
		}
		issue, err := store.Create(input)
		if err != nil {
			fail(w, err)
			return
		}
		jsonResponse(w, 201, issue)
	})
	mux.HandleFunc("PATCH /issues/{id}/status", func(w http.ResponseWriter, r *http.Request) {
		var body struct {
			Status string `json:"status"`
		}
		if err := decode(r, &body); err != nil {
			jsonResponse(w, 400, map[string]string{"error": "invalid JSON request"})
			return
		}
		issue, err := store.UpdateStatus(r.PathValue("id"), body.Status)
		if err != nil {
			fail(w, err)
			return
		}
		jsonResponse(w, 200, issue)
	})
	mux.HandleFunc("DELETE /issues/{id}", func(w http.ResponseWriter, r *http.Request) {
		if err := store.Delete(r.PathValue("id")); err != nil {
			fail(w, err)
			return
		}
		w.WriteHeader(http.StatusNoContent)
	})
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
		if r.Method == "OPTIONS" {
			w.WriteHeader(204)
			return
		}
		mux.ServeHTTP(w, r)
	})
}

func main() {
	path := os.Getenv("DATA_FILE")
	if strings.TrimSpace(path) == "" {
		path = "data/issues.json"
	}
	store, err := NewStore(path)
	if err != nil {
		log.Fatal(err)
	}
	// 问题列表为空时，创建一条演示异常。
	if len(store.List()) == 0 {
		_, err := store.Create(CreateIssue{
			Title:       "机柜 A-01 温度异常",
			Description: "设备温度过高，请现场检查。",
			Priority:    "high",
			DeviceID:    "A-01",
			DeviceName:  "机柜 A-01",
			Position: Position{
				X: -2.8,
				Y: 1.3,
				Z: 5.01,
			},
		})
		if err != nil {
			log.Fatal("创建演示异常失败：", err)
		}
	}
	log.Print("listening on :8080")
	log.Fatal(http.ListenAndServe(":8080", NewHandler(store)))
}
