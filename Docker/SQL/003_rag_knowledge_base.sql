-- 1. Таблица Баз Знаний (Knowledge Bases)
CREATE TABLE IF NOT EXISTS knowledge_bases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_public BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_kb_user_id ON knowledge_bases(user_id);
CREATE INDEX idx_kb_is_public ON knowledge_bases(is_public);

-- 2. Таблица Документов (принадлежат конкретной Базе Знаний)
CREATE TABLE IF NOT EXISTS documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    kb_id UUID NOT NULL REFERENCES knowledge_bases(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_content TEXT NOT NULL, -- Храним полный текст для простоты в консольном приложении
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_doc_kb_id ON documents(kb_id);


-- 3. Таблица Чанков (Самое важное для RAG)
CREATE TABLE IF NOT EXISTS document_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INT NOT NULL, -- Порядок чанка в документе
    breadcrumb_path TEXT, -- Метаданные: "H1: Введение > H2: Настройка"
    chunk_text TEXT NOT NULL,
    embedding vector(1024) -- Размерность bge-m3
);

-- Индекс HNSW для молниеносного косинусного поиска (требует pgvector)
CREATE INDEX idx_chunk_embedding ON document_chunks USING hnsw (embedding vector_cosine_ops);
CREATE INDEX idx_chunk_doc_id ON document_chunks(document_id);


-- 4. Таблица-связка: Подключенные Базы Знаний к Чату (M:N)
CREATE TABLE IF NOT EXISTS chat_session_kbs (
    chat_session_id UUID NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
    kb_id UUID NOT NULL REFERENCES knowledge_bases(id) ON DELETE CASCADE,
    PRIMARY KEY (chat_session_id, kb_id)
);

