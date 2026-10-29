package main

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"testing"
)

func TestIssueAPI(t *testing.T) {
	store, err := NewStore(filepath.Join(t.TempDir(), "issues.json"))
	if err != nil {
		t.Fatal(err)
	}
	h := NewHandler(store)
	request := func(method, path, body string) *httptest.ResponseRecorder {
		r := httptest.NewRequest(method, path, bytes.NewBufferString(body))
		w := httptest.NewRecorder()
		h.ServeHTTP(w, r)
		return w
	}
	if w := request("GET", "/health", ""); w.Code != http.StatusOK {
		t.Fatalf("health: %d", w.Code)
	}
	for _, body := range []string{`{"title":" ","priority":"high"}`, `{"title":"x","priority":"urgent"}`, `{invalid}`} {
		if w := request("POST", "/issues", body); w.Code != 400 {
			t.Fatalf("create %s: %d", body, w.Code)
		}
	}
	w := request("POST", "/issues", `{"title":"设备异常","priority":"high","position":{"x":1,"y":2,"z":3}}`)
	if w.Code != http.StatusCreated {
		t.Fatalf("create: %d %s", w.Code, w.Body)
	}
	var item Issue
	if err := json.Unmarshal(w.Body.Bytes(), &item); err != nil {
		t.Fatal(err)
	}
	if item.Status != "open" || item.Position.Z != 3 {
		t.Fatalf("item: %+v", item)
	}
	if w := request("PATCH", "/issues/"+item.ID+"/status", `{"status":"invalid"}`); w.Code != 400 {
		t.Fatalf("invalid status: %d", w.Code)
	}
	if w := request("PATCH", "/issues/missing/status", `{"status":"open"}`); w.Code != 404 {
		t.Fatalf("missing: %d", w.Code)
	}
	if w := request("PATCH", "/issues/"+item.ID+"/status", `{"status":"resolved"}`); w.Code != 200 {
		t.Fatalf("update: %d", w.Code)
	}
	w = request("GET", "/issues", "")
	var items []Issue
	if err := json.Unmarshal(w.Body.Bytes(), &items); err != nil {
		t.Fatal(err)
	}
	if len(items) != 1 || items[0].Status != "resolved" {
		t.Fatalf("list: %s", w.Body)
	}
	if w := request("OPTIONS", "/issues", ""); w.Code != 204 || w.Header().Get("Access-Control-Allow-Origin") == "" {
		t.Fatalf("CORS: %d %v", w.Code, w.Header())
	}
}
