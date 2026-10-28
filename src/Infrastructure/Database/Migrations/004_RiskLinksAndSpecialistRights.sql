-- Migration 004: closed risk–process links, RISK_SPECIALIST rights

-- Split and transfer close excluded links instead of losing them (08 §5)
ALTER TABLE risk.risk_processes
    ADD COLUMN IF NOT EXISTS unlinked_at timestamptz NULL;

-- 001 granted the specialist rights to 'RESK_SPECIALIST'; give them to the real role (03 §5)
INSERT INTO roles.role_rights (role_id, right_id)
SELECT ro.role_id, r.right_id
FROM roles.roles ro
JOIN roles.rights r ON r.right_code IN
    ('view_risk', 'comment_components', 'accept_risk', 'approve_treatment', 'work_report')
WHERE ro.role_code = 'RISK_SPECIALIST'
  AND NOT EXISTS (
      SELECT 1 FROM roles.role_rights rr
      WHERE rr.role_id = ro.role_id AND rr.right_id = r.right_id);
