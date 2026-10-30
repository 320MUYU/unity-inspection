package main

import (
	"bytes"
	"encoding/json"
	"errors"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"
)

func TestDeviceFieldsAndLegacyRecords(t *testing.T) {
	path := filepath.Join(t.TempDir(), "issues.json")
	legacy := `[{"id":"old","title":"旧问题","priority":"low","status":"open","position":{"x":0,"y":0,"z":0}}]`
	if err := os.WriteFile(path, []byte(legacy), 0600); err != nil {
		t.Fatal(err)
	}
	store, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	if items := store.List(); len(items) != 1 || items[0].DeviceID != "" || items[0].DeviceName != "" {
		t.Fatalf("legacy issue: %+v", items)
	}
	created, err := store.Create(CreateIssue{Title: "异常", Priority: "high", DeviceID: "A-01", DeviceName: "机柜 A-01"})
	if err != nil {
		t.Fatal(err)
	}
	if created.DeviceID != "A-01" || created.DeviceName != "机柜 A-01" {
		t.Fatalf("device: %+v", created)
	}
	reopened, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	if items := reopened.List(); len(items) != 2 || items[1].DeviceID != "A-01" || items[1].DeviceName != "机柜 A-01" {
		t.Fatalf("persisted: %+v", items)
	}
}

func TestDeleteRequiresResolvedAndPersists(t *testing.T) {
	path := filepath.Join(t.TempDir(), "issues.json")
	store, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	issue, err := store.Create(CreateIssue{Title: "问题", Priority: "low"})
	if err != nil {
		t.Fatal(err)
	}
	if err := store.Delete(issue.ID); !errors.Is(err, ErrConflict) {
		t.Fatalf("open delete: %v", err)
	}
	if _, err := store.UpdateStatus(issue.ID, "in_progress"); err != nil {
		t.Fatal(err)
	}
	if err := store.Delete(issue.ID); !errors.Is(err, ErrConflict) {
		t.Fatalf("in-progress delete: %v", err)
	}
	if _, err := store.UpdateStatus(issue.ID, "resolved"); err != nil {
		t.Fatal(err)
	}
	if err := store.Delete(issue.ID); err != nil {
		t.Fatal(err)
	}
	if err := store.Delete(issue.ID); !errors.Is(err, ErrNotFound) {
		t.Fatalf("missing delete: %v", err)
	}
	reopened, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	if len(reopened.List()) != 0 {
		t.Fatalf("deleted item persisted: %+v", reopened.List())
	}
}

func TestDeleteAPIAndDeviceRoundTrip(t *testing.T) {
	store, err := NewStore(filepath.Join(t.TempDir(), "issues.json"))
	if err != nil {
		t.Fatal(err)
	}
	h := NewHandler(store)
	call := func(method, path, body string) *httptest.ResponseRecorder {
		w := httptest.NewRecorder()
		h.ServeHTTP(w, httptest.NewRequest(method, path, bytes.NewBufferString(body)))
		return w
	}
	w := call("POST", "/issues", `{"title":"故障","priority":"medium","deviceId":"A-02","deviceName":"机柜 A-02","position":{"x":1,"y":2,"z":3}}`)
	if w.Code != http.StatusCreated {
		t.Fatalf("create: %d %s", w.Code, w.Body)
	}
	var item Issue
	if err := json.Unmarshal(w.Body.Bytes(), &item); err != nil {
		t.Fatal(err)
	}
	if item.DeviceID != "A-02" || item.DeviceName != "机柜 A-02" {
		t.Fatalf("device: %+v", item)
	}
	if w := call("DELETE", "/issues/"+item.ID, ""); w.Code != http.StatusConflict {
		t.Fatalf("open delete: %d %s", w.Code, w.Body)
	}
	if w := call("PATCH", "/issues/"+item.ID+"/status", `{"status":"resolved"}`); w.Code != 200 {
		t.Fatalf("resolve: %d", w.Code)
	}
	if w := call("DELETE", "/issues/"+item.ID, ""); w.Code != http.StatusNoContent {
		t.Fatalf("delete: %d %s", w.Code, w.Body)
	}
	if w := call("DELETE", "/issues/"+item.ID, ""); w.Code != http.StatusNotFound {
		t.Fatalf("missing: %d", w.Code)
	}
	if w := call("OPTIONS", "/issues/"+item.ID, ""); !bytes.Contains([]byte(w.Header().Get("Access-Control-Allow-Methods")), []byte("DELETE")) {
		t.Fatalf("CORS: %v", w.Header())
	}
}
