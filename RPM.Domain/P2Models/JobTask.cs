namespace RPM.Domain.P2Models
{
    /// <summary>
    /// workflow에서의 task를 나타냅니다.
    /// </summary>
    public class JobTask
    {
        /// <summary>
        /// JobTask의 이름을 가져오거나 설정합니다.
        /// JobTask를 식별하기 위한 Identifier. 하나의 DAG에서 노드들의 Name은 고유해야 합니다.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 다음 실행 노드들의 Name 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<string>? Next { get; set; }

        /// <summary>
        /// 실행하는 파일의 경로 또는 명령을 가져오거나 설정합니다
        /// </summary>
        public string? Command { get; set; }

        /// <summary>
        /// 표준 출력 값을 담을 변수를 가져오거나 설정합니다.
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// 표준 입력으로 사용 될 변수들을 가져오거나 설정합니다.
        /// </summary>
        public List<string>? Inputs { get; set; }

        /// <summary>
        /// 오류 발생 시 다시 실행하는 횟수를 가져오거나 설정합니다.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// 동작 활성화 여부를 가져오거나 설정합니다.
        /// 비활성으로 설정되면 하위 의존성을 가진 JobTask는 실행되지 않습니다.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
