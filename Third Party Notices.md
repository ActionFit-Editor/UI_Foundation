# Third Party Notices

이 문서는 UI Foundation 1.0.0의 구현에 개념적으로 참고된 외부 자료와 선택적 외부 연동 범위를 기록합니다.

## SlicedFilledImage concept

- Author: yasirkula
- Source: <https://gist.github.com/yasirkula/391fa12bc173acdf5ac48c466f180708>
- Permission: 저자는 2021-05-26 위 gist의 댓글에서 해당 코드를 0BSD license로 사용할 수 있다고 명시했습니다.
- Package use: `Runtime/Image_Slice.cs`와 `Runtime/Image_Slice.Mesh.cs`는 Unity UI에서 sliced image와 linear filled image를 결합하는 일반 개념(네 방향 fill과 `fillCenter` 동작 포함)을 참고해 `UnityEngine.UI.Image` subclass로 구현했습니다.

이 고지는 패키지 파일이 gist 원문을 그대로 복제했다는 주장이 아닙니다. 현재 구현은 3x3 slice cell과 fill 구간의 교차를 계산하는 패키지 자체 구조를 사용하며, 위 자료의 개념적 출처와 저자의 0BSD 허용 사실을 투명하게 남기기 위한 것입니다.

## DOTween

DOTween은 UI Foundation에 포함되거나 재배포되지 않습니다. 패키지는 소비 프로젝트가 별도로 공급한 DOTween core와 `DOTWEEN` scripting symbol이 모두 존재할 때만 조건부 API 호출을 컴파일합니다. DOTween을 설치하는 소비자는 자신이 취득한 배포본의 라이선스와 사용 조건을 별도로 확인해야 합니다.
