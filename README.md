# GallJungSan
디시인사이드 갤러리 개념글을 간단하게 정산해주는 프로그램입니다

GallJungSan.ini 파일을 수정하면 다른 갤러리에서도 사용할 수 있습니다


ini 파일 세팅 방법
GallCode : 해당 갤러리의 링크에 있는 id 값. ex) 소녀전선2 = gfl2, 중세게임 = aoegame
IsMgall : 해당 갤러리가 마이너 갤러리인지 bool 값
StartDate : 정산을 시작할 날짜. c#의 DateTime.Parse 형식에 맞게만 맞추면 됨. 모르겠으면 기본 ini 파일에서 날짜만 수정
EndDate : 정산을 끝낼 날자. 세팅은 위와 동일
ViewChromeLog : 크롬 로그를 볼지 bool 값
OpenChromeWindow : 크롬 창을 볼지 bool 값
