-- Migration 002: Add is_comment_active to risk.comments
ALTER TABLE risk.comments
    ADD COLUMN is_comment_active boolean NOT NULL DEFAULT true;
