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

---

## 프론트엔드 통합 (예시)

### React에서 게임 데이터 가져오기
```javascript
// 최근 게임 세션 가져오기
const fetchRecentGames = async () => {
  try {
    const response = await fetch('http://localhost:3000/api/game/recent', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error fetching games:', error);
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
