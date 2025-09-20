-- SQL Schema for Polling-Bee Application
-- Based on current Entity Framework models

-- Create polls table
CREATE TABLE IF NOT EXISTS polls (
    id SERIAL PRIMARY KEY,
    question TEXT NOT NULL,
    max_response_options INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by TEXT
);

-- Create poll_options table (without votes column - calculated dynamically)
CREATE TABLE IF NOT EXISTS poll_options (
    id SERIAL PRIMARY KEY,
    poll_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Foreign key constraint
    CONSTRAINT fk_poll_options_poll_id 
        FOREIGN KEY (poll_id) 
        REFERENCES polls(id) 
        ON DELETE CASCADE
);

-- Create poll_submissions table
CREATE TABLE IF NOT EXISTS poll_submissions (
    id SERIAL PRIMARY KEY,
    user_id TEXT NOT NULL,
    poll_id INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Foreign key constraint
    CONSTRAINT fk_poll_submissions_poll_id 
        FOREIGN KEY (poll_id) 
        REFERENCES polls(id) 
        ON DELETE CASCADE,
    
    -- Unique constraint to prevent duplicate submissions
    CONSTRAINT uq_poll_submissions_user_poll 
        UNIQUE (user_id, poll_id)
);

-- Create poll_submission_selections table (junction table)
CREATE TABLE IF NOT EXISTS poll_submission_selections (
    poll_submission_id INTEGER NOT NULL,
    poll_option_id INTEGER NOT NULL,
    
    -- Composite primary key
    PRIMARY KEY (poll_submission_id, poll_option_id),
    
    -- Foreign key constraints
    CONSTRAINT fk_poll_submission_selections_submission_id 
        FOREIGN KEY (poll_submission_id) 
        REFERENCES poll_submissions(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT fk_poll_submission_selections_option_id 
        FOREIGN KEY (poll_option_id) 
        REFERENCES poll_options(id) 
        ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_poll_options_poll_id ON poll_options(poll_id);
CREATE INDEX IF NOT EXISTS idx_poll_submissions_poll_id ON poll_submissions(poll_id);
CREATE INDEX IF NOT EXISTS idx_poll_submissions_user_id ON poll_submissions(user_id);
CREATE INDEX IF NOT EXISTS idx_poll_submission_selections_submission_id ON poll_submission_selections(poll_submission_id);
CREATE INDEX IF NOT EXISTS idx_poll_submission_selections_option_id ON poll_submission_selections(poll_option_id);

-- Optional: Insert sample data
INSERT INTO polls (question, max_response_options) VALUES 
('What is your favorite color?', 1),
('What is your favorite country?', 2)
ON CONFLICT DO NOTHING;

-- Get the poll IDs for inserting options
DO $$
DECLARE
    poll1_id INTEGER;
    poll2_id INTEGER;
BEGIN
    SELECT id INTO poll1_id FROM polls WHERE question = 'What is your favorite color?' LIMIT 1;
    SELECT id INTO poll2_id FROM polls WHERE question = 'What is your favorite country?' LIMIT 1;
    
    IF poll1_id IS NOT NULL THEN
        INSERT INTO poll_options (poll_id, name) VALUES 
        (poll1_id, 'Red'),
        (poll1_id, 'Blue')
        ON CONFLICT DO NOTHING;
    END IF;
    
    IF poll2_id IS NOT NULL THEN
        INSERT INTO poll_options (poll_id, name) VALUES 
        (poll2_id, 'USA'),
        (poll2_id, 'Canada'),
        (poll2_id, 'UK'),
        (poll2_id, 'Australia')
        ON CONFLICT DO NOTHING;
    END IF;
END $$;

-- Verify the schema
SELECT 
    t.table_name,
    c.column_name,
    c.data_type,
    c.is_nullable,
    c.column_default
FROM information_schema.tables t
JOIN information_schema.columns c ON t.table_name = c.table_name
WHERE t.table_schema = 'public' 
    AND t.table_name IN ('polls', 'poll_options', 'poll_submissions', 'poll_submission_selections')
ORDER BY t.table_name, c.ordinal_position;
