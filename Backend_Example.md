# 백엔드 구현 예시 (Node.js + Express)

게임 점수 제출 API를 위한 백엔드 구현 예시입니다.

## 1. 프로젝트 설정

```bash
mkdir infinite-stairs-backend
cd infinite-stairs-backend
npm init -y
npm install express mysql2 cors dotenv
```

## 2. 서버 기본 구조

### server.js

```javascript
const express = require("express");
const cors = require("cors");
const mysql = require("mysql2/promise");
require("dotenv").config();

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(cors());
app.use(express.json());

// Database connection pool
const pool = mysql.createPool({
  host: process.env.DB_HOST || "localhost",
  user: process.env.DB_USER || "root",
  password: process.env.DB_PASSWORD || "",
  database: process.env.DB_NAME || "infinite_stairs",
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
});

// Routes
const gameRoutes = require("./routes/game");
const scoreRoutes = require("./routes/score");

app.use("/api/game", gameRoutes);
app.use("/api/score", scoreRoutes);

// Health check
app.get("/api/health", (req, res) => {
  res.json({ status: "ok", message: "Server is running" });
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});

module.exports = { pool };
```

## 3. 점수 제출 API 구현

### routes/score.js

```javascript
const express = require("express");
const router = express.Router();
const { pool } = require("../server");

// 캐릭터 이름 매핑
const CHARACTER_NAMES = {
  0: "BusinessMan",
  1: "Rapper",
  2: "Secretary",
  3: "Boxer",
  4: "CheerLeader",
  5: "Sheriff",
  6: "Plumber",
};

// ★ 점수 제출 API (가장 중요)
router.post("/submit", async (req, res) => {
  try {
    const { score, characterIndex, money, timestamp } = req.body;

    // 유효성 검사
    if (score === undefined || characterIndex === undefined) {
      return res.status(400).json({
        success: false,
        message: "필수 필드가 누락되었습니다",
        error: "score와 characterIndex는 필수입니다",
      });
    }

    if (score < 0 || characterIndex < 0 || characterIndex > 6) {
      return res.status(400).json({
        success: false,
        message: "잘못된 데이터입니다",
        error: "score는 0 이상, characterIndex는 0-6 사이여야 합니다",
      });
    }

    const characterName = CHARACTER_NAMES[characterIndex];
    const connection = await pool.getConnection();

    try {
      // 트랜잭션 시작
      await connection.beginTransaction();

      // 점수 저장
      const [insertResult] = await connection.execute(
        `INSERT INTO scores (score, character_index, character_name, money, timestamp)
         VALUES (?, ?, ?, ?, ?)`,
        [
          score,
          characterIndex,
          characterName,
          money || 0,
          timestamp || new Date(),
        ]
      );

      // 현재 점수의 순위 계산
      const [rankResult] = await connection.execute(
        "SELECT COUNT(*) + 1 as rank FROM scores WHERE score > ?",
        [score]
      );
      const rank = rankResult[0].rank;

      // 최고 점수 조회 (user_id가 있다면 사용자별로 조회)
      const [bestScoreResult] = await connection.execute(
        "SELECT MAX(score) as bestScore FROM scores"
      );
      const bestScore = bestScoreResult[0].bestScore || 0;

      // 개인 최고 기록 갱신 여부 (여기서는 전체 최고 기록으로 간단히 구현)
      const isNewBestScore = score === bestScore;

      await connection.commit();

      res.json({
        success: true,
        message: "점수가 성공적으로 저장되었습니다",
        data: {
          rank: rank,
          isNewBestScore: isNewBestScore,
          bestScore: bestScore,
        },
      });
    } catch (error) {
      await connection.rollback();
      throw error;
    } finally {
      connection.release();
    }
  } catch (error) {
    console.error("점수 제출 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

// 최근 점수 기록 조회
router.get("/recent", async (req, res) => {
  try {
    const limit = parseInt(req.query.limit) || 10;

    const [rows] = await pool.execute(
      `SELECT id, score, character_index, character_name, money, timestamp
       FROM scores
       ORDER BY timestamp DESC
       LIMIT ?`,
      [limit]
    );

    res.json({
      success: true,
      data: rows,
    });
  } catch (error) {
    console.error("최근 점수 조회 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

// 리더보드 조회
router.get("/leaderboard", async (req, res) => {
  try {
    const limit = parseInt(req.query.limit) || 100;

    const [rows] = await pool.execute(
      `SELECT
        ROW_NUMBER() OVER (ORDER BY score DESC) as rank,
        id, score, character_index, character_name, money, timestamp
       FROM scores
       ORDER BY score DESC
       LIMIT ?`,
      [limit]
    );

    res.json({
      success: true,
      data: rows,
    });
  } catch (error) {
    console.error("리더보드 조회 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

// 캐릭터별 통계
router.get("/character-stats", async (req, res) => {
  try {
    const [rows] = await pool.execute(
      `SELECT
        character_index,
        character_name,
        COUNT(*) as total_games,
        AVG(score) as avg_score,
        MAX(score) as max_score,
        SUM(money) as total_money
       FROM scores
       GROUP BY character_index, character_name
       ORDER BY character_index`
    );

    res.json({
      success: true,
      data: rows,
    });
  } catch (error) {
    console.error("캐릭터 통계 조회 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

// 전체 통계
router.get("/stats", async (req, res) => {
  try {
    const [totalGames] = await pool.execute(
      "SELECT COUNT(*) as count FROM scores"
    );

    const [avgScore] = await pool.execute(
      "SELECT AVG(score) as average FROM scores"
    );

    const [maxScore] = await pool.execute(
      "SELECT MAX(score) as max FROM scores"
    );

    const [totalMoney] = await pool.execute(
      "SELECT SUM(money) as total FROM scores"
    );

    res.json({
      success: true,
      data: {
        totalGames: totalGames[0].count,
        averageScore: Math.round(avgScore[0].average || 0),
        maxScore: maxScore[0].max || 0,
        totalMoney: totalMoney[0].total || 0,
      },
    });
  } catch (error) {
    console.error("전체 통계 조회 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

module.exports = router;
```

### routes/game.js

```javascript
const express = require("express");
const router = express.Router();
const { pool } = require("../server");
const { v4: uuidv4 } = require("uuid");

// 게임 시작
router.post("/start", async (req, res) => {
  try {
    const { status } = req.body;

    if (status !== 1) {
      return res.status(400).json({
        success: false,
        message: "잘못된 요청입니다",
        error: "status는 1이어야 합니다",
      });
    }

    const sessionId = uuidv4();
    const startTime = new Date();

    const [result] = await pool.execute(
      "INSERT INTO game_sessions (id, status, start_time) VALUES (?, ?, ?)",
      [sessionId, status, startTime]
    );

    res.json({
      success: true,
      message: "게임이 시작되었습니다",
      data: {
        sessionId: sessionId,
        startTime: startTime.toISOString(),
      },
    });
  } catch (error) {
    console.error("게임 시작 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

// 게임 종료
router.post("/end", async (req, res) => {
  try {
    const { status, stairCount } = req.body;

    if (status !== 0) {
      return res.status(400).json({
        success: false,
        message: "잘못된 요청입니다",
        error: "status는 0이어야 합니다",
      });
    }

    if (stairCount === undefined) {
      return res.status(400).json({
        success: false,
        message: "잘못된 요청입니다",
        error: "stairCount는 필수입니다",
      });
    }

    res.json({
      success: true,
      message: "게임 결과가 저장되었습니다",
      data: {
        stairCount: stairCount,
        endTime: new Date().toISOString(),
      },
    });
  } catch (error) {
    console.error("게임 종료 오류:", error);
    res.status(500).json({
      success: false,
      message: "서버 오류가 발생했습니다",
      error: error.message,
    });
  }
});

module.exports = router;
```

## 4. 데이터베이스 초기화

### database/init.sql

```sql
-- 데이터베이스 생성
CREATE DATABASE IF NOT EXISTS infinite_stairs;
USE infinite_stairs;

-- 게임 세션 테이블
CREATE TABLE IF NOT EXISTS game_sessions (
  id VARCHAR(36) PRIMARY KEY,
  user_id VARCHAR(36),
  status INT NOT NULL,
  stair_count INT DEFAULT 0,
  start_time TIMESTAMP NOT NULL,
  end_time TIMESTAMP,
  play_duration INT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- ★ 점수 테이블 (가장 중요)
CREATE TABLE IF NOT EXISTS scores (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id VARCHAR(36),
  score INT NOT NULL,
  character_index INT NOT NULL,
  character_name VARCHAR(50),
  money INT DEFAULT 0,
  timestamp TIMESTAMP NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_score (score DESC),
  INDEX idx_user_score (user_id, score DESC),
  INDEX idx_timestamp (timestamp DESC),
  INDEX idx_character (character_index)
);

-- 사용자 통계 테이블
CREATE TABLE IF NOT EXISTS user_stats (
  user_id VARCHAR(36) PRIMARY KEY,
  best_score INT DEFAULT 0,
  total_games INT DEFAULT 0,
  total_money INT DEFAULT 0,
  average_score FLOAT DEFAULT 0,
  last_played TIMESTAMP,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

## 5. 환경 변수 설정

### .env

```
PORT=3000
DB_HOST=localhost
DB_USER=root
DB_PASSWORD=your_password
DB_NAME=infinite_stairs
```

## 6. 실행

```bash
# 데이터베이스 초기화
mysql -u root -p < database/init.sql

# 서버 실행
node server.js
```

## 7. 테스트

```bash
# 점수 제출 테스트
curl -X POST http://localhost:3000/api/score/submit \
  -H "Content-Type: application/json" \
  -d '{
    "score": 150,
    "characterIndex": 2,
    "money": 45,
    "timestamp": "2025-11-19T20:35:00Z"
  }'

# 리더보드 조회
curl http://localhost:3000/api/score/leaderboard?limit=10

# 최근 점수 조회
curl http://localhost:3000/api/score/recent?limit=5

# 캐릭터 통계 조회
curl http://localhost:3000/api/score/character-stats

# 전체 통계 조회
curl http://localhost:3000/api/score/stats
```
