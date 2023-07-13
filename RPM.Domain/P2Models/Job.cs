namespace RPM.Domain.P2Models
{
    /// <summary>
    /// workflow에서 Job을 나타냅니다
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Job의 이름을 가져오거나 설정합니다.
        /// </summary>
        public string? JobName { get; set; }

        /// <summary>
        /// Task가 실패할 경우, 재시도 횟수를 가져오거나 설정합니다.
        /// 설정 값은 Job의 JobTask에 전파됩니다.
        /// JobTask.Retry가 재정의 되어있다면 JobTask.Retry가 우선적으로 적용됩니다.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// 노드의 의존성과 상관없이 workflow 전역에서 사용할 수 있는 값을 설정합니다.
        /// task는 task의 inputs 속성에 key를 설정하여 저장된 value를 command 동작에 주입할 수 있습니다.
        /// </summary>
        public Dictionary<string, string>? InputValues { get; set; }

        /// <summary>
        /// Job에 등록 된 JobTask들의 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<JobTask>? JobTasks { get; set; }
    }
}
