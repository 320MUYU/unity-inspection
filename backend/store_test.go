package main

import (
	"path/filepath"
	"sync"
	"testing"
)

func TestStorePersistsIssuesAndStatus(t *testing.T) {
	path := filepath.Join(t.TempDir(), "issues.json")
	store, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	issue, err := store.Create(CreateIssue{Title: "  机柜异常  ", Priority: "high", Position: Position{X: 1.25, Y: 0.8, Z: 2.4}})
	if err != nil {
		t.Fatal(err)
	}
	if issue.Title != "机柜异常" || issue.Status != "open" || issue.ID == "" || issue.CreatedAt.IsZero() || issue.UpdatedAt.IsZero() {
		t.Fatalf("unexpected issue: %+v", issue)
	}
	if _, err := store.UpdateStatus(issue.ID, "resolved"); err != nil {
		t.Fatal(err)
	}
	reopened, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	items := reopened.List()
	if len(items) != 1 || items[0].ID != issue.ID || items[0].Status != "resolved" || items[0].Position.X != 1.25 {
		t.Fatalf("unexpected persisted issues: %+v", items)
	}
}

func TestStoreRejectsInvalidValues(t *testing.T) {
	store, err := NewStore(filepath.Join(t.TempDir(), "issues.json"))
	if err != nil {
		t.Fatal(err)
	}
	for _, input := range []CreateIssue{{Title: " ", Priority: "low"}, {Title: "x", Priority: "urgent"}} {
		if _, err := store.Create(input); err == nil {
			t.Fatalf("accepted invalid input: %+v", input)
		}
	}
	item, err := store.Create(CreateIssue{Title: "x", Priority: "low"})
	if err != nil {
		t.Fatal(err)
	}
	if _, err := store.UpdateStatus(item.ID, "invalid"); err == nil {
		t.Fatal("accepted invalid status")
	}
	if _, err := store.UpdateStatus("missing", "open"); err == nil {
		t.Fatal("accepted missing ID")
	}
}

func TestConcurrentCreatesPersist(t *testing.T) {
	path := filepath.Join(t.TempDir(), "issues.json")
	store, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	var wg sync.WaitGroup
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			if _, err := store.Create(CreateIssue{Title: "test", Priority: "medium"}); err != nil {
				t.Error(err)
			}
		}()
	}
	wg.Wait()
	reopened, err := NewStore(path)
	if err != nil {
		t.Fatal(err)
	}
	if got := len(reopened.List()); got != 10 {
		t.Fatalf("got %d issues, want 10", got)
	}
}
