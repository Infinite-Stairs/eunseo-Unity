# Infinite Stairs API 문서

## 개요
유니티 게임과 백엔드 서버 간 통신을 위한 API 명세서입니다.

## Base URL
```
http://localhost:3000/api
```
> 실제 배포 시 프로덕션 URL로 변경해야 합니다.

---

## 1. 게임 시작 API

### Endpoint
```
POST /api/game/start
```

### Request Body
```json
{
  "status": 1
}
```

### Request Fields
| Field | Type | Description |
|-------|------|-------------|
| status | Integer | 게임 시작 상태값 (항상 1) |

### Response (Success)
```json
{
  "success": true,
  "message": "게임이 시작되었습니다",
  "data": {
    "sessionId": "uuid-generated-session-id",
    "startTime": "2025-11-19T20:30:00Z"
  }
}
```

### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| success | Boolean | 요청 성공 여부 |
| message | String | 응답 메시지 |
| data.sessionId | String | 게임 세션 ID |
| data.startTime | String | 게임 시작 시간 (ISO 8601) |

---

## 2. 게임 종료 API

### Endpoint
```
POST /api/game/end
```

### Request Body
```json
{
  "status": 0,
  "stairCount": 150
}
```

### Request Fields
| Field | Type | Description |
|-------|------|-------------|
| status | Integer | 게임 종료 상태값 (항상 0) |
| stairCount | Integer | 플레이어가 도달한 계단 수 (점수) |

### Response (Success)
```json
{
  "success": true,
  "message": "게임 결과가 저장되었습니다",
  "data": {
    "sessionId": "uuid-generated-session-id",
    "stairCount": 150,
    "endTime": "2025-11-19T20:35:00Z",
    "playTime": "00:05:00",
    "rank": 15,
    "isNewRecord": false
  }
}
```

### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| success | Boolean | 요청 성공 여부 |
| message | String | 응답 메시지 |
| data.sessionId | String | 게임 세션 ID |
| data.stairCount | Integer | 기록된 계단 수 |
| data.endTime | String | 게임 종료 시간 (ISO 8601) |
| data.playTime | String | 플레이 시간 (HH:MM:SS) |
| data.rank | Integer | 현재 순위 (옵션) |
| data.isNewRecord | Boolean | 최고 기록 갱신 여부 (옵션) |

---

## 3. 점수 제출 API (★ 중요)

### Endpoint
```
POST /api/score/submit
```

### 설명
게임 한 판이 끝날 때마다 호출되는 API입니다. 점수, 캐릭터 정보, 획득 코인 등을 DB에 저장하고 프론트엔드에서 활용할 수 있습니다.

### Request Body
```json
{
  "score": 150,
  "characterIndex": 2,
  "money": 45,
  "timestamp": "2025-11-19T20:35:00Z"
}
```

### Request Fields
| Field | Type | Description |
|-------|------|-------------|
| score | Integer | 플레이어가 도달한 계단 수 (점수) |
| characterIndex | Integer | 사용한 캐릭터 인덱스 (0-6) |
| money | Integer | 게임에서 획득한 코인 수 |
| timestamp | String | 게임 종료 시간 (ISO 8601 형식) |

### 캐릭터 인덱스 매핑
| Index | Character Name | Korean Name |
|-------|---------------|-------------|
| 0 | BusinessMan | 회사원 |
| 1 | Rapper | 래퍼 |
| 2 | Secretary | 비서 |
| 3 | Boxer | 복서 |
| 4 | CheerLeader | 치어리더 |
| 5 | Sheriff | 보안관 |
| 6 | Plumber | 배관공 |

### Response (Success)
```json
{
  "success": true,
  "message": "점수가 성공적으로 저장되었습니다",
  "data": {
    "rank": 15,
    "isNewBestScore": false,
    "bestScore": 150
  }
}
```

### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| success | Boolean | 요청 성공 여부 |
| message | String | 응답 메시지 |
| data.rank | Integer | 현재 점수의 전체 순위 |
| data.isNewBestScore | Boolean | 개인 최고 기록 갱신 여부 |
| data.bestScore | Integer | 현재까지의 개인 최고 점수 |

### 사용 예시 (Unity)
```csharp
// GameOver 함수에서 자동으로 호출됨
int characterIndex = dslManager.GetSelectedCharIndex();
apiManager.SubmitScore(score, characterIndex, player.money, (success, scoreData) => {
    if (success && scoreData != null) {
        Debug.Log($"점수: {score}, 순위: {scoreData.rank}");
        if (scoreData.isNewBestScore) {
            Debug.Log("새로운 최고 기록!");
        }
    }
});
```

---

## Error Responses

### 400 Bad Request
```json
{
  "success": false,
  "message": "잘못된 요청입니다",
  "error": "stairCount는 필수 필드입니다"
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "서버 오류가 발생했습니다",
  "error": "Database connection failed"
}
```

---

## 데이터베이스 스키마 (예시)

### GameSession Table
```sql
CREATE TABLE game_sessions (
  id VARCHAR(36) PRIMARY KEY,
  user_id VARCHAR(36),
  status INT NOT NULL,  -- 1: 진행중, 0: 종료
  stair_count INT DEFAULT 0,
  start_time TIMESTAMP NOT NULL,
  end_time TIMESTAMP,
  play_duration INT,  -- seconds
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### Scores Table (★ 중요 - 점수 제출 API용)
```sql
CREATE TABLE scores (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id VARCHAR(36),  -- 유저 식별자 (옵션)
  score INT NOT NULL,
  character_index INT NOT NULL,
  character_name VARCHAR(50),
  money INT DEFAULT 0,
  timestamp TIMESTAMP NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_score (score DESC),
  INDEX idx_user_score (user_id, score DESC),
  INDEX idx_timestamp (timestamp DESC)
);
```

### UserStats Table (사용자 통계용)
```sql
CREATE TABLE user_stats (
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

---

## Unity 사용 예시

### APIManager 설정
1. 유니티 씬에 빈 GameObject 생성 (이름: APIManager)
2. APIManager.cs 스크립트 연결
3. GameManager에서 APIManager 참조 설정

### 백엔드 URL 변경
```csharp
apiManager.SetBaseURL("https://your-production-url.com/api");
```

### 게임 시작 호출
```csharp
apiManager.SendGameStart((success, response) => {
    if (success) {
        Debug.Log("게임 시작 성공");
    }
});
```

### 게임 종료 호출
```csharp
int finalScore = 150;
apiManager.SendGameEnd(finalScore, (success, response) => {
    if (success) {
        Debug.Log("게임 종료 성공");
    }
});
```

### 점수 제출 호출 (★ 가장 중요)
```csharp
// GameOver 시 자동으로 호출됨
int characterIndex = dslManager.GetSelectedCharIndex();
apiManager.SubmitScore(score, characterIndex, player.money, (success, scoreData) => {
    if (success && scoreData != null) {
        Debug.Log($"점수 제출 성공!");
        Debug.Log($"순위: {scoreData.rank}");
        Debug.Log($"최고 기록: {scoreData.bestScore}");
    }
});
```

---

## 프론트엔드 통합 (예시)

### React에서 게임 데이터 가져오기
```javascript
// 최근 점수 기록 가져오기 (점수 제출 API로 저장된 데이터)
const fetchRecentScores = async (limit = 10) => {
  try {
    const response = await fetch(`http://localhost:3000/api/scores/recent?limit=${limit}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    const data = await response.json();
    return data;
    // 예상 응답:
    // {
    //   success: true,
    //   data: [
    //     { id: 1, score: 250, characterIndex: 2, money: 78, timestamp: "2025-11-19..." },
    //     { id: 2, score: 180, characterIndex: 0, money: 45, timestamp: "2025-11-19..." },
    //     ...
    //   ]
    // }
  } catch (error) {
    console.error('Error fetching scores:', error);
  }
};

// 전체 랭킹 가져오기
const fetchLeaderboard = async (limit = 100) => {
  try {
    const response = await fetch(`http://localhost:3000/api/scores/leaderboard?limit=${limit}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    const data = await response.json();
    return data;
    // 예상 응답:
    // {
    //   success: true,
    //   data: [
    //     { rank: 1, score: 500, characterName: "비서", money: 150, ... },
    //     { rank: 2, score: 450, characterName: "회사원", money: 120, ... },
    //     ...
    //   ]
    // }
  } catch (error) {
    console.error('Error fetching leaderboard:', error);
  }
};

// 캐릭터별 통계 가져오기
const fetchCharacterStats = async () => {
  try {
    const response = await fetch('http://localhost:3000/api/scores/character-stats', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    const data = await response.json();
    return data;
    // 예상 응답:
    // {
    //   success: true,
    //   data: [
    //     { characterIndex: 0, characterName: "회사원", totalGames: 120, avgScore: 180 },
    //     { characterIndex: 1, characterName: "래퍼", totalGames: 85, avgScore: 165 },
    //     ...
    //   ]
    // }
  } catch (error) {
    console.error('Error fetching character stats:', error);
  }
};

// 사용자 통계 가져오기
const fetchUserStats = async (userId) => {
  try {
    const response = await fetch(`http://localhost:3000/api/user/${userId}/stats`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error fetching stats:', error);
  }
};
```

---

## 추가 개선 사항 (옵션)

1. **유저 인증 추가**
   - 게임 시작 시 유저 토큰 전송
   - JWT 기반 인증 시스템

2. **실시간 랭킹**
   - WebSocket을 이용한 실시간 순위 업데이트

3. **게임 통계**
   - 평균 플레이 시간
   - 최고 기록
   - 일일 플레이 횟수

4. **오프라인 지원**
   - 네트워크 끊김 시 로컬에 저장
   - 재연결 시 자동 동기화
