-- Migration 003: link risk_processes to the process, seed the built-in assessment method

ALTER TABLE risk.risk_processes
    ADD COLUMN IF NOT EXISTS process_id integer NULL REFERENCES org.processes(process_id);

CREATE INDEX IF NOT EXISTS ix_risk_processes_process
    ON risk.risk_processes(process_id);

-- Built-in method "Матрица рисков" (R = L × I); the services use method_id = 1
INSERT INTO assessment.methods (method_id, method_title, method_description)
VALUES (1, 'Матрица рисков', 'R = L × I')
ON CONFLICT DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('assessment.methods', 'method_id'),
    GREATEST((SELECT MAX(method_id) FROM assessment.methods), 1));

INSERT INTO assessment.likelihood_levels (method_id, likelihood_level_name, likelihood_level_weight)
VALUES
    (1, 'Невозможно', 0),
    (1, 'Почти невозможно', 1),
    (1, 'Маловероятно', 2),
    (1, 'Возможно', 3),
    (1, 'Вероятно', 4),
    (1, 'Почти неизбежно', 5)
ON CONFLICT DO NOTHING;

-- Money thresholds are not fixed by the requirements; the administrator changes them
INSERT INTO assessment.impact_levels
    (method_id, impact_level_name, impact_level_weight, impact_level_loss, impact_level_max)
VALUES
    (1, 'Нет влияния', 0, 0, 9999.99),
    (1, 'Незначительная', 1, 10000, 49999.99),
    (1, 'Низкая', 2, 50000, 199999.99),
    (1, 'Средняя', 3, 200000, 499999.99),
    (1, 'Высокая', 4, 500000, 999999.99),
    (1, 'Критическая', 5, 1000000, 9999999999999999.99)
ON CONFLICT DO NOTHING;

INSERT INTO assessment.risk_acceptance_settings
    (method_id, owner_max_score, director_max_score, ceo_max_score)
VALUES (1, 10, 15, 19)
ON CONFLICT DO NOTHING;

INSERT INTO treatment.treatment_approval_settings
    (owner_threshold, director_threshold, ceo_threshold)
SELECT 0, 50000, 100000
WHERE NOT EXISTS (SELECT 1 FROM treatment.treatment_approval_settings);
