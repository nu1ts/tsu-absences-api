using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tsu_absences_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionReasonToAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Absences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Absences");
        }
    }
}
