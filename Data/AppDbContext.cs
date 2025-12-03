using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Models;

namespace SkillShareBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserType> UserTypes { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<StudyGroup> StudyGroups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupMessage> GroupMessages { get; set; }
    public DbSet<MessageReaction> MessageReactions { get; set; }
    public DbSet<MessageReadStatus> MessageReadStatuses { get; set; }

    public DbSet<Call> GroupCalls { get; set; }
    public DbSet<CallParticipant> CallParticipants { get; set; }
    public DbSet<CallStatistics> CallStatistics { get; set; }

    public DbSet<GroupDocument> GroupDocuments { get; set; }
    public DbSet<DocumentFavorite> DocumentFavorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración para Call (GROUP_CALLS)
        modelBuilder.Entity<Call>(entity =>
        {
            entity.ToTable("GROUP_CALLS");
            entity.HasKey(c => c.Id).HasName("PRIMARY");
            entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(c => c.CallId).HasColumnName("call_id").IsRequired().HasMaxLength(100);
            entity.Property(c => c.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(c => c.StartedBy).HasColumnName("started_by").IsRequired().HasMaxLength(100);
            entity.Property(c => c.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(c => c.EndedAt).HasColumnName("ended_at");
            entity.Property(c => c.EndedBy).HasColumnName("ended_by").HasMaxLength(100);
            entity.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(c => c.ParticipantCount).HasColumnName("participant_count").HasDefaultValue(0);

            entity.HasIndex(c => c.CallId).IsUnique().HasDatabaseName("IX_Call_CallId");
            entity.HasIndex(c => new { c.GroupId, c.IsActive }).HasDatabaseName("IX_Call_GroupId_IsActive");

            entity.HasOne(c => c.Group)
                .WithMany()
                .HasForeignKey(c => c.GroupId)
                .HasConstraintName("FK_Call_Group")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración para CallParticipant
        modelBuilder.Entity<CallParticipant>(entity =>
        {
            entity.ToTable("CALL_PARTICIPANTS");
            entity.HasKey(cp => cp.Id).HasName("PRIMARY");
            entity.Property(cp => cp.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(cp => cp.CallId).HasColumnName("call_id").IsRequired().HasMaxLength(100);
            entity.Property(cp => cp.UserId).HasColumnName("user_id").IsRequired().HasMaxLength(100);
            entity.Property(cp => cp.JoinedAt).HasColumnName("joined_at").IsRequired();
            entity.Property(cp => cp.LeftAt).HasColumnName("left_at");

            entity.HasIndex(cp => new { cp.CallId, cp.UserId }).HasDatabaseName("IX_CallParticipant_CallId_UserId");

            entity.HasOne(cp => cp.Call)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.CallId)
                .HasPrincipalKey(c => c.CallId)
                .HasConstraintName("FK_CallParticipant_Call")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración para CallStatistics
        modelBuilder.Entity<CallStatistics>(entity =>
        {
            entity.ToTable("CALL_STATISTICS");
            entity.HasKey(cs => cs.Id).HasName("PRIMARY");
            entity.Property(cs => cs.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(cs => cs.CallId).HasColumnName("call_id").IsRequired().HasMaxLength(100);
            entity.Property(cs => cs.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(cs => cs.TotalParticipants).HasColumnName("total_participants").HasDefaultValue(0);
            entity.Property(cs => cs.AverageDuration).HasColumnName("average_duration").HasDefaultValue(0);
            entity.Property(cs => cs.MaxParticipants).HasColumnName("max_participants").HasDefaultValue(0);
            entity.Property(cs => cs.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(cs => cs.Call)
                .WithMany()
                .HasForeignKey(cs => cs.CallId)
                .HasPrincipalKey(c => c.CallId)
                .HasConstraintName("FK_CallStatistics_Call")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cs => cs.Group)
                .WithMany()
                .HasForeignKey(cs => cs.GroupId)
                .HasConstraintName("FK_CallStatistics_Group")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración para User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.UserId).HasName("PRIMARY");
            entity.Property(u => u.UserId).HasColumnName("user_id").ValueGeneratedOnAdd();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.Property(u => u.Password).HasColumnName("password").IsRequired().HasMaxLength(255);
            entity.Property(u => u.ProfileImage).HasColumnName("profile_image").HasMaxLength(500).IsRequired(false);
            entity.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        });

        // Configuración para UserType
        modelBuilder.Entity<UserType>(entity =>
        {
            entity.ToTable("user_type");
            entity.HasKey(ut => ut.Id).HasName("PRIMARY");
            entity.Property(ut => ut.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(ut => ut.Type).HasColumnName("type").IsRequired().HasMaxLength(50);
            entity.HasIndex(ut => ut.Type).IsUnique();
        });

        // Configuración para Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("student");
            entity.HasKey(s => s.Id).HasName("PRIMARY");
            entity.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(s => s.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            entity.Property(s => s.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            entity.Property(s => s.Nickname).HasColumnName("nickname").HasMaxLength(100).IsRequired(false);
            entity.Property(s => s.DateBirth).HasColumnName("date_birth").IsRequired(false);
            entity.Property(s => s.Country).HasColumnName("country").HasMaxLength(100).IsRequired(false);
            entity.Property(s => s.EducationalCenter).HasColumnName("educational_center").HasMaxLength(150)
                .IsRequired(false);
            entity.Property(s => s.Gender).HasColumnName("gender").HasMaxLength(10).HasDefaultValue("other");
            entity.Property(s => s.UserType).HasColumnName("user_type").IsRequired(false);
            entity.Property(s => s.UserId).HasColumnName("user_id").IsRequired(false);

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .HasConstraintName("FK_Student_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<UserType>()
                .WithMany()
                .HasForeignKey(s => s.UserType)
                .HasConstraintName("FK_Student_UserType")
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración para Subject
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("subjects");
            entity.HasKey(s => s.Id).HasName("PRIMARY");
            entity.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        });

        // Configuración para StudyGroup
        modelBuilder.Entity<StudyGroup>(entity =>
        {
            entity.ToTable("study_groups");
            entity.HasKey(sg => sg.Id).HasName("PRIMARY");
            entity.Property(sg => sg.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(sg => sg.Name).HasColumnName("name").IsRequired().HasMaxLength(150);
            entity.Property(sg => sg.Description).HasColumnName("description").HasColumnType("TEXT");
            entity.Property(sg => sg.CoverImage).HasColumnName("cover_image").HasMaxLength(500);
            entity.Property(sg => sg.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(sg => sg.SubjectId).HasColumnName("subject_id");
            entity.Property(sg => sg.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(sg => sg.Creator)
                .WithMany()
                .HasForeignKey(sg => sg.CreatedBy)
                .HasConstraintName("FK_StudyGroup_Creator")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sg => sg.Subject)
                .WithMany(s => s.StudyGroups)
                .HasForeignKey(sg => sg.SubjectId)
                .HasConstraintName("FK_StudyGroup_Subject")
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración para GroupMember
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.ToTable("group_members");
            entity.HasKey(gm => gm.Id).HasName("PRIMARY");
            entity.Property(gm => gm.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(gm => gm.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(gm => gm.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(gm => gm.Role).HasColumnName("role").HasMaxLength(10).HasDefaultValue("member");

            entity.HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .HasConstraintName("FK_GroupMember_Group")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .HasConstraintName("FK_GroupMember_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(gm => new { gm.GroupId, gm.UserId })
                .IsUnique()
                .HasDatabaseName("IX_GroupMember_GroupId_UserId");
        });

        modelBuilder.Entity<GroupMessage>(entity =>
        {
            entity.ToTable("group_messages");
            entity.HasKey(gm => gm.Id).HasName("PRIMARY");
            entity.Property(gm => gm.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(gm => gm.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(gm => gm.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(gm => gm.MessageType).HasColumnName("message_type").HasMaxLength(20).IsRequired();
            entity.Property(gm => gm.Content).HasColumnName("content");
            entity.Property(gm => gm.FileUrl).HasColumnName("file_url").HasMaxLength(500);
            entity.Property(gm => gm.FileName).HasColumnName("file_name").HasMaxLength(255);
            entity.Property(gm => gm.FileSize).HasColumnName("file_size");
            entity.Property(gm => gm.Duration).HasColumnName("duration");
            entity.Property(gm => gm.ReplyToMessageId).HasColumnName("reply_to_message_id");
            entity.Property(gm => gm.IsEdited).HasColumnName("is_edited").HasDefaultValue(false);
            entity.Property(gm => gm.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(gm => gm.CreatedAt).HasColumnName("created_at");
            entity.Property(gm => gm.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(gm => gm.Group)
                .WithMany()
                .HasForeignKey(gm => gm.GroupId)
                .HasConstraintName("FK_GroupMessage_Group")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .HasConstraintName("FK_GroupMessage_User")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.ToTable("message_reactions");
            entity.HasKey(mr => mr.Id).HasName("PRIMARY");
            entity.Property(mr => mr.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(mr => mr.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(mr => mr.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(mr => mr.Reaction).HasColumnName("reaction").HasMaxLength(10).IsRequired();
            entity.Property(mr => mr.CreatedAt).HasColumnName("created_at");

            entity.HasOne(mr => mr.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(mr => mr.MessageId)
                .HasConstraintName("FK_MessageReaction_Message")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .HasConstraintName("FK_MessageReaction_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(mr => new { mr.MessageId, mr.UserId, mr.Reaction })
                .IsUnique()
                .HasDatabaseName("IX_MessageReaction_Unique");
        });

        modelBuilder.Entity<MessageReadStatus>(entity =>
        {
            entity.ToTable("message_read_status");
            entity.HasKey(mrs => mrs.Id).HasName("PRIMARY");
            entity.Property(mrs => mrs.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(mrs => mrs.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(mrs => mrs.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(mrs => mrs.ReadAt).HasColumnName("read_at");

            entity.HasOne(mrs => mrs.Message)
                .WithMany(m => m.ReadStatuses)
                .HasForeignKey(mrs => mrs.MessageId)
                .HasConstraintName("FK_MessageReadStatus_Message")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mrs => mrs.User)
                .WithMany()
                .HasForeignKey(mrs => mrs.UserId)
                .HasConstraintName("FK_MessageReadStatus_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(mrs => new { mrs.MessageId, mrs.UserId })
                .IsUnique()
                .HasDatabaseName("IX_MessageReadStatus_Unique");
        });

        modelBuilder.Entity<GroupDocument>(entity =>
        {
            entity.ToTable("group_documents");
            entity.HasKey(gd => gd.Id).HasName("PRIMARY");
            entity.Property(gd => gd.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(gd => gd.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(gd => gd.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(gd => gd.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(gd => gd.Description).HasColumnName("description");
            entity.Property(gd => gd.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(255);
            entity.Property(gd => gd.FileUrl).HasColumnName("file_url").IsRequired().HasMaxLength(500);
            entity.Property(gd => gd.FileSize).HasColumnName("file_size");
            entity.Property(gd => gd.FileType).HasColumnName("file_type").HasMaxLength(50);
            entity.Property(gd => gd.SubjectId).HasColumnName("subject_id");
            entity.Property(gd => gd.UploadDate).HasColumnName("upload_date");
            entity.Property(gd => gd.DownloadCount).HasColumnName("download_count").HasDefaultValue(0);
            entity.Property(gd => gd.FavoriteCount).HasColumnName("favorite_count").HasDefaultValue(0);

            entity.HasOne(gd => gd.Group)
                .WithMany()
                .HasForeignKey(gd => gd.GroupId)
                .HasConstraintName("FK_GroupDocument_Group")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gd => gd.User)
                .WithMany()
                .HasForeignKey(gd => gd.UserId)
                .HasConstraintName("FK_GroupDocument_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gd => gd.Subject)
                .WithMany()
                .HasForeignKey(gd => gd.SubjectId)
                .HasConstraintName("FK_GroupDocument_Subject")
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración para DocumentFavorite
        modelBuilder.Entity<DocumentFavorite>(entity =>
        {
            entity.ToTable("document_favorites");
            entity.HasKey(df => df.Id).HasName("PRIMARY");
            entity.Property(df => df.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(df => df.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(df => df.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(df => df.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(df => df.Document)
                .WithMany()
                .HasForeignKey(df => df.DocumentId)
                .HasConstraintName("FK_DocumentFavorite_Document")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(df => df.User)
                .WithMany()
                .HasForeignKey(df => df.UserId)
                .HasConstraintName("FK_DocumentFavorite_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(df => new { df.DocumentId, df.UserId })
                .IsUnique()
                .HasDatabaseName("IX_DocumentFavorite_DocumentId_UserId");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new Exception("Database update error occurred", ex);
        }
    }
}