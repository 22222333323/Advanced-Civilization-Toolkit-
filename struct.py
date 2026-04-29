from __future__ import annotations

import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional, Tuple


# =========================
# Модели
# =========================

@dataclass
class MethodNode:
    name: str
    return_type: str
    params: str
    kind: str = "method"  # method | constructor | operator | local_function


@dataclass
class ClassNode:
    kind: str  # class | struct | interface | record
    name: str
    start: int
    end: int
    methods: List[MethodNode] = field(default_factory=list)
    nested_types: List["ClassNode"] = field(default_factory=list)


# =========================
# Утилиты
# =========================

TYPE_DECL_RE = re.compile(
    r"""
    \b
    (?P<mods>(?:(?:public|private|protected|internal|static|abstract|sealed|partial|new|unsafe|readonly)\s+)*)?
    (?P<kind>class|struct|interface|record)\s+
    (?P<name>[A-Za-z_]\w*)
    (?:\s*<[^>{}]+>)?
    (?:\s*:\s*[^{]+)?
    \s*\{
    """,
    re.VERBOSE | re.MULTILINE,
)

# Пытаемся не ловить if/for/while/switch/using и т.п.
BLOCKED_PREFIXES = (
    "if", "for", "foreach", "while", "switch", "catch", "using",
    "lock", "return", "throw", "else", "do", "try", "fixed",
)

METHOD_RE = re.compile(
    r"""
    ^\s*
    (?P<mods>(?:(?:public|private|protected|internal|static|virtual|override|async|sealed|abstract|extern|partial|new|unsafe|readonly|ref)\s+)*)?
    (?P<ret>[A-Za-z_][\w<>,\[\]\?\.\s]*?)\s+
    (?P<name>[A-Za-z_]\w*)
    (?:\s*<(?P<gen>[^>\n]+)>)?
    \s*\(
        (?P<params>[^)]*)
    \)
    \s*(?P<suffix>\{|;|=>)?
    \s*$
    """,
    re.VERBOSE | re.MULTILINE,
)


def mask_comments_and_strings(src: str) -> str:
    """
    Заменяет комментарии и строки на пробелы, сохраняя длину текста.
    Это помогает искать классы/методы, не путая их с кодом внутри строк.
    """
    out = []
    i = 0
    n = len(src)

    NORMAL, LINE_COMMENT, BLOCK_COMMENT, STRING, VERBATIM_STRING, CHAR = range(6)
    state = NORMAL

    while i < n:
        ch = src[i]

        if state == NORMAL:
            # line comment //
            if ch == "/" and i + 1 < n and src[i + 1] == "/":
                out.append("  ")
                i += 2
                state = LINE_COMMENT
                continue

            # block comment /*
            if ch == "/" and i + 1 < n and src[i + 1] == "*":
                out.append("  ")
                i += 2
                state = BLOCK_COMMENT
                continue

            # regular or verbatim string
            if ch == '"':
                out.append(" ")
                i += 1
                state = STRING
                continue

            # char literal
            if ch == "'":
                out.append(" ")
                i += 1
                state = CHAR
                continue

            # possible verbatim string prefix @"..."
            if ch == "@" and i + 1 < n and src[i + 1] == '"':
                out.append("  ")
                i += 2
                state = VERBATIM_STRING
                continue

            # possible interpolated string prefix $"..."
            if ch == "$" and i + 1 < n and src[i + 1] == '"':
                out.append("  ")
                i += 2
                state = STRING
                continue

            # possible verbatim interpolated string prefix $@"..."
            if ch == "$" and i + 2 < n and src[i + 1] == "@" and src[i + 2] == '"':
                out.append("   ")
                i += 3
                state = VERBATIM_STRING
                continue

            out.append(ch)
            i += 1
            continue

        if state == LINE_COMMENT:
            if ch == "\n":
                out.append("\n")
                i += 1
                state = NORMAL
            else:
                out.append(" ")
                i += 1
            continue

        if state == BLOCK_COMMENT:
            if ch == "*" and i + 1 < n and src[i + 1] == "/":
                out.append("  ")
                i += 2
                state = NORMAL
            else:
                out.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if state == STRING:
            if ch == "\\" and i + 1 < n:
                out.append("  ")
                i += 2
                continue
            if ch == '"':
                out.append(" ")
                i += 1
                state = NORMAL
            else:
                out.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if state == VERBATIM_STRING:
            if ch == '"' and i + 1 < n and src[i + 1] == '"':
                out.append("  ")
                i += 2
                continue
            if ch == '"':
                out.append(" ")
                i += 1
                state = NORMAL
            else:
                out.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if state == CHAR:
            if ch == "\\" and i + 1 < n:
                out.append("  ")
                i += 2
                continue
            if ch == "'":
                out.append(" ")
                i += 1
                state = NORMAL
            else:
                out.append("\n" if ch == "\n" else " ")
                i += 1
            continue

    return "".join(out)


def find_matching_brace(masked_src: str, open_brace_index: int) -> int:
    depth = 0
    for i in range(open_brace_index, len(masked_src)):
        if masked_src[i] == "{":
            depth += 1
        elif masked_src[i] == "}":
            depth -= 1
            if depth == 0:
                return i
    return -1


def line_start_index(text: str, pos: int) -> int:
    return text.rfind("\n", 0, pos) + 1


def is_control_line(line: str) -> bool:
    stripped = line.lstrip()
    for prefix in BLOCKED_PREFIXES:
        if stripped.startswith(prefix + " ") or stripped.startswith(prefix + "(") or stripped == prefix:
            return True
    return False


def extract_methods(body_original: str, body_masked: str, class_name: str) -> List[MethodNode]:
    methods: List[MethodNode] = []
    lines = body_masked.splitlines(True)
    orig_lines = body_original.splitlines(True)

    offset = 0
    for mline, oline in zip(lines, orig_lines):
        stripped = mline.strip()
        if not stripped:
            offset += len(mline)
            continue

        # Только строки, похожие на сигнатуры
        if "(" not in stripped or ")" not in stripped:
            offset += len(mline)
            continue
        if is_control_line(stripped):
            offset += len(mline)
            continue

        # Конструктор
        ctor_re = re.compile(
            rf"""
            ^\s*
            (?P<mods>(?:(?:public|private|protected|internal|static|async|partial|unsafe)\s+)*)?
            (?P<name>{re.escape(class_name)})
            \s*\(
                (?P<params>[^)]*)
            \)
            \s*(?P<suffix>\{{|;|=>)?
            \s*$
            """,
            re.VERBOSE,
        )
        cm = ctor_re.match(stripped)
        if cm:
            methods.append(
                MethodNode(
                    name=cm.group("name"),
                    return_type="(constructor)",
                    params=cm.group("params").strip(),
                    kind="constructor",
                )
            )
            offset += len(mline)
            continue

        # Метод / operator / локальная функция
        mm = METHOD_RE.match(stripped)
        if mm:
            ret = mm.group("ret").strip()
            name = mm.group("name").strip()
            params = mm.group("params").strip()

            # Небольшая фильтрация: не тащим конструкции типа "namespace Foo"
            if ret not in {"namespace", "class", "struct", "interface", "record"}:
                kind = "method"
                if name == "operator":
                    kind = "operator"
                methods.append(
                    MethodNode(
                        name=name,
                        return_type=ret,
                        params=params,
                        kind=kind,
                    )
                )

        offset += len(mline)

    return methods


def parse_types(src: str, masked: str, start: int = 0, end: Optional[int] = None) -> List[ClassNode]:
    if end is None:
        end = len(src)

    nodes: List[ClassNode] = []
    i = start

    while i < end:
        m = TYPE_DECL_RE.search(masked, i, end)
        if not m:
            break

        kind = m.group("kind")
        name = m.group("name")

        open_brace = masked.find("{", m.end() - 1, end)
        if open_brace == -1:
            i = m.end()
            continue

        close_brace = find_matching_brace(masked, open_brace)
        if close_brace == -1 or close_brace > end:
            i = m.end()
            continue

        body_start = open_brace + 1
        body_end = close_brace

        body_original = src[body_start:body_end]
        body_masked = masked[body_start:body_end]

        node = ClassNode(
            kind=kind,
            name=name,
            start=m.start(),
            end=close_brace + 1,
        )

        # Сначала рекурсивно ищем вложенные типы
        node.nested_types = parse_types(src, masked, body_start, body_end)

        # Потом методы в теле
        # Чтобы вложенные типы не мешали, маскируем их диапазоны пробелами
        body_masked_for_methods = list(body_masked)
        for nested in node.nested_types:
            for j in range(nested.start - body_start, nested.end - body_start):
                if 0 <= j < len(body_masked_for_methods) and body_masked_for_methods[j] != "\n":
                    body_masked_for_methods[j] = " "
        body_masked_for_methods = "".join(body_masked_for_methods)

        node.methods = extract_methods(body_original, body_masked_for_methods, class_name=name)

        nodes.append(node)
        i = close_brace + 1

    return nodes


def print_tree(nodes: List[ClassNode], indent: int = 0) -> None:
    pad = "  " * indent
    for node in nodes:
        print(f"{pad}{node.kind} {node.name}")
        for method in node.methods:
            print(
                f"{pad}  - {method.kind}: {method.return_type} {method.name}({method.params})"
            )
        if node.nested_types:
            print_tree(node.nested_types, indent + 1)


def parse_csharp_file(path: str | Path) -> List[ClassNode]:
    path = Path(path)
    src = path.read_text(encoding="utf-8", errors="ignore")
    masked = mask_comments_and_strings(src)
    return parse_types(src, masked)


if __name__ == "__main__":
    file_path = Path("Class1.cs")  # ваш файл
    tree = parse_csharp_file(file_path)
    print_tree(tree)