using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudyFlow.Infrastructure;

#nullable disable

namespace StudyFlow.Infrastructure.Migrations
{
    [DbContext(typeof(PlannerDbContext))]
    [Migration("20261109113534_InitialSchema")]
    partial class InitialSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "10.0.14");

            modelBuilder.Entity("StudyFlow.Domain.Models.Course", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Archived")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ColorHex")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("TEXT");

                    b.Property<int>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Courses", null, t =>
                        {
                            t.HasCheckConstraint("CK_Course_Name", "length(trim(Name)) BETWEEN 1 AND 80");
                        });
                });

            modelBuilder.Entity("StudyFlow.Domain.Models.StudyTask", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("CourseId")
                        .HasColumnType("TEXT");

                    b.Property<DateOnly>("DueDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("EstimatedMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Importance")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.HasIndex("Status", "DueDate");

                    b.ToTable("Tasks", null, t =>
                        {
                            t.HasCheckConstraint("CK_Task_Importance", "Importance BETWEEN 1 AND 5");

                            t.HasCheckConstraint("CK_Task_Minutes", "EstimatedMinutes BETWEEN 15 AND 1440");

                            t.HasCheckConstraint("CK_Task_Status", "Status BETWEEN 0 AND 2");

                            t.HasCheckConstraint("CK_Task_Title", "length(trim(Title)) BETWEEN 1 AND 120");
                        });
                });

            modelBuilder.Entity("StudyFlow.Domain.Models.StudyTask", b =>
                {
                    b.HasOne("StudyFlow.Domain.Models.Course", null)
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
