#!/bin/bash
# Fires on Stop. If api/ files were modified in this session, injects a mandatory
# reminder that Claude must run backend-ddd-reviewer before the session is done.
if { git diff --name-only origin/main...HEAD -- api/ 2>/dev/null
     git diff --name-only HEAD -- api/ 2>/dev/null
     git diff --name-only --cached -- api/ 2>/dev/null
   } | grep -q .; then
  cat <<'EOF'
{"continue":false,"stopReason":"MANDATORY: Backend files under api/ were modified in this session. You MUST invoke the backend-ddd-reviewer agent now before this session is considered done. This is a non-negotiable quality gate — do not skip it."}
EOF
fi
