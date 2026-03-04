import subprocess
import os

TARGET_MODELS = [
    "AdventureWorks Internet Operation",
    "AdventureWorks Inventory",
    "AdventureWorks Reseller Operation",
]

TABULAR_EDITOR = r"c:\Program Files (x86)\Tabular Editor\TabularEditor.exe"
SCRIPT = "master-model-pattern.csx"
TMDL_SOURCE = "TMDL Source"
SERVER = "localhost"

BLUE = "\033[94m"
CYAN = "\033[96m"
GREEN = "\033[92m"
RED = "\033[91m"
BOLD = "\033[1m"
RESET = "\033[0m"


def get_perspective(model_name: str) -> str:
    return "$" + model_name.removeprefix("AdventureWorks ")


def print_header():
    print()
    print(f"{BLUE}{BOLD}{'=' * 60}")
    print(f"   Deploying Master Model Pattern Sample")
    print(f"{'=' * 60}{RESET}")
    print()


def print_sub_header(model_name: str, index: int, total: int):
    print(f"  {CYAN}{BOLD}[{index}/{total}] Deploying: {model_name}{RESET}")


def print_footer(success_count: int, total: int):
    print()
    color = GREEN if success_count == total else RED
    print(f"{color}{BOLD}{'=' * 60}")
    print(f"   Done — {success_count}/{total} models deployed successfully")
    print(f"{'=' * 60}{RESET}")
    print()


def main():
    print_header()

    success_count = 0
    total = len(TARGET_MODELS)

    for i, model in enumerate(TARGET_MODELS, start=1):
        perspective = get_perspective(model)
        os.environ["TE_DevPerspective"] = perspective

        print_sub_header(model, i, total)

        result = subprocess.run(
            [TABULAR_EDITOR, TMDL_SOURCE, "-S", SCRIPT, "-D", SERVER, model, "-O"],
        )

        if result.returncode == 0:
            print(f"  {GREEN}  Success{RESET}")
        else:
            print(f"  {RED}  Failed (exit code {result.returncode}){RESET}")

        success_count += result.returncode == 0
        print()

    print_footer(success_count, total)


if __name__ == "__main__":
    main()
