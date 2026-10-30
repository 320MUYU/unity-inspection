package main

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

var ErrInvalid = errors.New("invalid input")
var ErrNotFound = errors.New("issue not found")
var ErrConflict = errors.New("only resolved issues can be deleted")

type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}
type CreateIssue struct {
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	DeviceID    string   `json:"deviceId"`
	DeviceName  string   `json:"deviceName"`
	Position    Position `json:"position"`
}
type Issue struct {
	ID          string    `json:"id"`
	Title       string    `json:"title"`
	Description string    `json:"description"`
	Priority    string    `json:"priority"`
	Status      string    `json:"status"`
	DeviceID    string    `json:"deviceId"`
	DeviceName  string    `json:"deviceName"`
	Position    Position  `json:"position"`
	CreatedAt   time.Time `json:"createdAt"`
	UpdatedAt   time.Time `json:"updatedAt"`
}
type Store struct {
	mu     sync.Mutex
	path   string
	issues []Issue
}

func NewStore(path string) (*Store, error) {
	s := &Store{path: path, issues: []Issue{}}
	data, err := os.ReadFile(path)
	if errors.Is(err, os.ErrNotExist) {
		return s, nil
	}
	if err != nil {
		return nil, err
	}
	if err = json.Unmarshal(data, &s.issues); err != nil {
		return nil, fmt.Errorf("read issues: %w", err)
	}
	if s.issues == nil {
		s.issues = []Issue{}
	}
	return s, nil
}

func (s *Store) persist(issues []Issue) error {
	if err := os.MkdirAll(filepath.Dir(s.path), 0755); err != nil {
		return err
	}
	data, err := json.MarshalIndent(issues, "", "  ")
	if err != nil {
		return err
	}
	f, err := os.CreateTemp(filepath.Dir(s.path), ".issues-*.tmp")
	if err != nil {
		return err
	}
	name := f.Name()
	defer os.Remove(name)
	if _, err = f.Write(data); err != nil {
		f.Close()
		return err
	}
	if err = f.Sync(); err != nil {
		f.Close()
		return err
	}
	if err = f.Close(); err != nil {
		return err
	}
	return os.Rename(name, s.path)
}

func (s *Store) Create(input CreateIssue) (Issue, error) {
	input.Title = strings.TrimSpace(input.Title)
	if input.Title == "" {
		return Issue{}, fmt.Errorf("%w: title is required", ErrInvalid)
	}
	if input.Priority != "low" && input.Priority != "medium" && input.Priority != "high" {
		return Issue{}, fmt.Errorf("%w: invalid priority", ErrInvalid)
	}
	var id [16]byte
	if _, err := rand.Read(id[:]); err != nil {
		return Issue{}, err
	}
	now := time.Now().UTC()
	item := Issue{ID: hex.EncodeToString(id[:]), Title: input.Title, Description: input.Description, Priority: input.Priority, Status: "open", DeviceID: input.DeviceID, DeviceName: input.DeviceName, Position: input.Position, CreatedAt: now, UpdatedAt: now}
	s.mu.Lock()
	defer s.mu.Unlock()
	next := append(append([]Issue{}, s.issues...), item)
	if err := s.persist(next); err != nil {
		return Issue{}, err
	}
	s.issues = next
	return item, nil
}

func (s *Store) List() []Issue {
	s.mu.Lock()
	defer s.mu.Unlock()
	return append([]Issue{}, s.issues...)
}

func (s *Store) UpdateStatus(id, status string) (Issue, error) {
	if status != "open" && status != "in_progress" && status != "resolved" {
		return Issue{}, fmt.Errorf("%w: invalid status", ErrInvalid)
	}
	s.mu.Lock()
	defer s.mu.Unlock()
	next := append([]Issue{}, s.issues...)
	for i := range next {
		if next[i].ID == id {
			next[i].Status = status
			next[i].UpdatedAt = time.Now().UTC()
			if err := s.persist(next); err != nil {
				return Issue{}, err
			}
			s.issues = next
			return next[i], nil
		}
	}
	return Issue{}, ErrNotFound
}

func (s *Store) Delete(id string) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	for i, issue := range s.issues {
		if issue.ID != id {
			continue
		}
		if issue.Status != "resolved" {
			return ErrConflict
		}
		next := make([]Issue, 0, len(s.issues)-1)
		next = append(next, s.issues[:i]...)
		next = append(next, s.issues[i+1:]...)
		if err := s.persist(next); err != nil {
			return err
		}
		s.issues = next
		return nil
	}
	return ErrNotFound
}
