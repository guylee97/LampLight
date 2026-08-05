import sys
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) != 4:
        print("usage: summarize_tests.py <results.xml> <mode> <failures.txt>", file=sys.stderr)
        return 2

    path, mode, out = sys.argv[1], sys.argv[2], sys.argv[3]
    root = ET.parse(path).getroot()

    total = root.get("total")
    passed = root.get("passed")
    failed = root.get("failed")
    print(f"  {mode}: {passed}/{total} 통과, {failed} 실패")

    names = []
    for case in root.iter("test-case"):
        if case.get("result") != "Passed":
            names.append(f"{mode} {case.get('fullname')}")

    if len(names) != int(failed):
        print(f"  경고: 실패 {failed} 건인데 이름 {len(names)} 개만 모았다", file=sys.stderr)
        return 1

    with open(out, "a") as handle:
        for name in names:
            handle.write(name + "\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
