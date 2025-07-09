CREATE TABLE IF NOT EXISTS users (
    id VARCHAR(40) PRIMARY KEY,
    name VARCHAR(40) NOT NULL,
    password VARCHAR(100) NOT NULL,
    is_verified BOOLEAN NOT NULL
);

INSERT INTO users (id, name, password, is_verified)
VALUES
    ('1', 'mark', 'mark', TRUE),
    ('2', 'ayk', 'ayk', TRUE),
    ('3', 'yarik', 'yarik', TRUE),
    ('4', 'nikita', 'nikita', TRUE);