namespace SkillShareBackend.DTOs
{
    public class GroupMessageDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public string MessageType { get; set; } = "text";
        public string? Content { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public int? Duration { get; set; }
        public int? ReplyToMessageId { get; set; }
        public ReplyMessageDto? ReplyToMessage { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageReactionDto> Reactions { get; set; } = new();
        public bool IsRead { get; set; }
        public bool IsSentByCurrentUser { get; set; }
    }

    public class ReplyMessageDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public string? Content { get; set; }
        public string? FileName { get; set; }
    }

    public class SendMessageDto
    {
        public int GroupId { get; set; }
        public string MessageType { get; set; } = "text";
        public string? Content { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public int? Duration { get; set; }
        public int? ReplyToMessageId { get; set; }
    }

    public class UpdateMessageDto
    {
        public string? Content { get; set; }
    }

    public class MessageReactionDto
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Reaction { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AddReactionDto
    {
        public string Reaction { get; set; } = string.Empty;
    }
}