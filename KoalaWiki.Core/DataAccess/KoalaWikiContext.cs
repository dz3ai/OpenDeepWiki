﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KoalaWiki.Domains;
using KoalaWiki.Domains.DocumentFile;
using KoalaWiki.Domains.FineTuning;
using KoalaWiki.Domains.MCP;
using KoalaWiki.Domains.Users;
using KoalaWiki.Domains.Warehouse;
using KoalaWiki.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoalaWiki.Core.DataAccess;

public class KoalaWikiContext<TContext>(DbContextOptions<TContext> options)
    : DbContext(options), IKoalaWikiContext where TContext : DbContext
{
    public DbSet<Warehouse> Warehouses { get; set; }

    public DbSet<DocumentCatalog> DocumentCatalogs { get; set; }

    public DbSet<Document> Documents { get; set; }

    public DbSet<DocumentFileItem> DocumentFileItems { get; set; }

    public DbSet<DocumentFileItemSource> DocumentFileItemSources { get; set; }

    public DbSet<DocumentOverview> DocumentOverviews { get; set; }

    public DbSet<DocumentCommitRecord> DocumentCommitRecords { get; set; }

    public DbSet<ChatShareMessage> ChatShareMessages { get; set; }

    public DbSet<ChatShareMessageItem> ChatShareMessageItems { get; set; }

    public DbSet<TrainingDataset> TrainingDatasets { get; set; }

    public DbSet<FineTuningTask> FineTuningTasks { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<MCPHistory> MCPHistories { get; set; }

    public DbSet<UserInAuth> UserInAuths { get; set; }

    public DbSet<UserInRole> UserInRoles { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<WarehouseInRole> WarehouseInRoles { get; set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        BeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        BeforeSaveChanges();
        return base.SaveChanges();
    }

    private void BeforeSaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: ICreateEntity creationAudited })
            {
                creationAudited.CreatedAt = DateTime.UtcNow;
            }
        }
    }

    public async Task RunMigrateAsync()
    {
        await Database.MigrateAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Warehouse>((builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasComment("主键Id");
            builder.Property(x => x.Name).IsRequired().HasComment("仓库名称");
            builder.Property(x => x.Description).IsRequired().HasComment("仓库描述");
            builder.Property(x => x.CreatedAt).IsRequired().HasComment("创建时间");
            builder.Property(x => x.Status).HasComment("仓库状态");
            builder.Property(x => x.Address).HasComment("仓库地址");
            builder.Property(x => x.Type).HasComment("仓库类型");
            builder.Property(x => x.Branch).HasComment("分支");
            builder.Property(x => x.OrganizationName).HasComment("组织名称");
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.Address);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.Branch);
            builder.HasIndex(x => x.OrganizationName);
            builder.HasComment("知识仓库表");
        }));

        modelBuilder.Entity<DocumentCatalog>((builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasComment("主键Id");
            builder.Property(x => x.Name).IsRequired().HasComment("目录名称");
            builder.Property(x => x.Description).IsRequired().HasComment("目录描述");
            builder.Property(x => x.CreatedAt).IsRequired().HasComment("创建时间");
            builder.Property(x => x.ParentId).HasComment("父级目录Id");
            builder.Property(x => x.WarehouseId).HasComment("所属仓库Id");
            builder.Property(x => x.DucumentId).HasComment("文档Id");
            builder.Property(x => x.IsDeleted).HasComment("是否已删除");
            builder.Property(x => x.DependentFile)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => string.IsNullOrEmpty(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                .HasComment("依赖文件");
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.ParentId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.DucumentId);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasComment("文档目录表");
        }));

        modelBuilder.Entity<Document>((builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasComment("主键Id");
            builder.Property(x => x.Description).IsRequired().HasComment("文档描述");
            builder.Property(x => x.CreatedAt).IsRequired().HasComment("创建时间");
            builder.Property(x => x.WarehouseId).HasComment("所属仓库Id");
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasComment("文档表");
        }));

        modelBuilder.Entity<DocumentFileItem>((builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasComment("主键Id");
            builder.Property(x => x.Title).IsRequired().HasComment("文件标题");
            builder.Property(x => x.Description).IsRequired().HasComment("文件描述");
            builder.Property(x => x.CreatedAt).IsRequired().HasComment("创建时间");
            builder.Property(x => x.DocumentCatalogId).HasComment("目录Id");
            builder.Property(x => x.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null))
                .HasComment("元数据");
            builder.Property(x => x.Extra)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null))
                .HasComment("扩展信息");
            builder.HasIndex(x => x.Title);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.DocumentCatalogId);
            builder.HasComment("文档文件项表");
        }));

        modelBuilder.Entity<DocumentFileItemSource>((builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasComment("主键Id");
            builder.Property(x => x.Name).IsRequired().HasComment("来源名称");
            builder.Property(x => x.CreatedAt).IsRequired().HasComment("创建时间");
            builder.Property(x => x.DocumentFileItemId).HasComment("文件项Id");
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.DocumentFileItemId);
            builder.HasComment("文档文件项来源表");
        }));

        modelBuilder.Entity<DocumentOverview>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.Title).IsRequired().HasComment("文档标题");
            options.Property(x => x.DocumentId).HasComment("文档Id");
            options.HasIndex(x => x.DocumentId);
            options.HasIndex(x => x.Title);
            options.HasComment("文档概览表");
        });

        modelBuilder.Entity<DocumentCommitRecord>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.CommitMessage).IsRequired().HasComment("提交信息");
            options.Property(x => x.Author).IsRequired().HasComment("作者");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.Property(x => x.CommitId).HasComment("提交Id");
            options.HasIndex(x => x.WarehouseId);
            options.HasIndex(x => x.CommitId);
            options.HasComment("文档提交记录表");
        });

        modelBuilder.Entity<ChatShareMessage>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.HasIndex(x => x.WarehouseId);
            options.HasComment("聊天分享消息表");
        });

        modelBuilder.Entity<ChatShareMessageItem>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.ChatShareMessageId).HasComment("聊天分享消息Id");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.Property(x => x.Question).IsRequired().HasComment("问题内容");
            options.Property(x => x.Files)
                .HasConversion(x => JsonSerializer.Serialize(x, (JsonSerializerOptions)null),
                    x => JsonSerializer.Deserialize<List<string>>(x, (JsonSerializerOptions)null))
                .HasComment("相关文件");
            options.HasIndex(x => x.ChatShareMessageId);
            options.HasIndex(x => x.WarehouseId);
            options.HasIndex(x => x.Question);
            options.HasComment("聊天分享消息项表");
        });

        modelBuilder.Entity<User>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.Name).IsRequired().HasComment("用户名");
            options.Property(x => x.Email).IsRequired().HasComment("邮箱");
            options.Property(x => x.Password).IsRequired().HasComment("密码");
            options.Property(x => x.CreatedAt).HasComment("创建时间");
            options.Property(x => x.LastLoginAt).HasComment("最后登录时间");
            options.HasIndex(x => x.Name);
            options.HasIndex(x => x.Email);
            options.HasIndex(x => x.CreatedAt);
            options.HasIndex(x => x.LastLoginAt);
            options.HasComment("用户表");
        });

        modelBuilder.Entity<TrainingDataset>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.Name).IsRequired().HasComment("数据集名称");
            options.Property(x => x.CreatedAt).HasComment("创建时间");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.HasIndex(x => x.Name);
            options.HasIndex(x => x.CreatedAt);
            options.HasIndex(x => x.WarehouseId);
            options.HasComment("训练数据集表");
        });

        modelBuilder.Entity<FineTuningTask>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.Name).IsRequired().HasComment("微调任务名称");
            options.Property(x => x.CreatedAt).HasComment("创建时间");
            options.Property(x => x.TrainingDatasetId).HasComment("训练数据集Id");
            options.Property(x => x.UserId).HasComment("用户Id");
            options.Property(x => x.Status).HasComment("任务状态");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.Property(x => x.DocumentCatalogId).HasComment("目录Id");
            options.HasIndex(x => x.Name);
            options.HasIndex(x => x.CreatedAt);
            options.HasIndex(x => x.TrainingDatasetId);
            options.HasIndex(x => x.UserId);
            options.HasIndex(x => x.Status);
            options.HasIndex(x => x.WarehouseId);
            options.HasIndex(x => x.DocumentCatalogId);
            options.HasOne(x => x.DocumentCatalog)
                .WithMany()
                .HasForeignKey(x => x.DocumentCatalogId)
                .OnDelete(DeleteBehavior.Cascade);
            options.HasComment("微调任务表");
        });

        modelBuilder.Entity<MCPHistory>(options =>
        {
            options.HasComment("MCP历史记录");
            options.Property(x => x.Id).HasComment("主键Id");
            options.HasKey(x => x.Id);
            options.Property(x => x.CreatedAt).HasComment("创建时间");
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.Property(x => x.UserId).HasComment("用户Id");
            options.HasIndex(x => x.CreatedAt);
            options.HasIndex(x => x.WarehouseId);
            options.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<UserInAuth>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.UserId).IsRequired().HasComment("用户Id");
            options.Property(x => x.Provider).IsRequired().HasComment("认证提供方");
            options.Property(x => x.AuthId).IsRequired().HasComment("认证Id");
            options.HasIndex(x => x.UserId);
            options.HasIndex(x => x.Provider);
            options.HasIndex(x => x.AuthId);
            options.HasComment("用户认证表");
        });

        modelBuilder.Entity<UserInRole>(options =>
        {
            options.HasKey(x => new { x.UserId, x.RoleId });
            options.Property(x => x.UserId).HasComment("用户Id");
            options.Property(x => x.RoleId).HasComment("角色Id");
            options.HasComment("用户角色关联表");
        });

        modelBuilder.Entity<Role>(options =>
        {
            options.HasKey(x => x.Id);
            options.Property(x => x.Id).HasComment("主键Id");
            options.Property(x => x.Name).IsRequired().HasComment("角色名称");
            options.Property(x => x.Description).IsRequired().HasComment("角色描述");
            options.Property(x => x.CreatedAt).HasComment("创建时间");
            options.HasIndex(x => x.Name);
            options.HasIndex(x => x.CreatedAt);
            options.HasComment("角色表");
        });

        modelBuilder.Entity<WarehouseInRole>(options =>
        {
            options.HasKey(x => new { x.WarehouseId, x.RoleId });
            options.Property(x => x.WarehouseId).HasComment("仓库Id");
            options.Property(x => x.RoleId).HasComment("角色Id");
            options.HasIndex(x => x.WarehouseId);
            options.HasIndex(x => x.RoleId);
            options.HasComment("仓库角色关联表");
        });
    }
}